// Copyright 2021 Flavien Charlon
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace SecretConfiguration.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

public class SecretConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IConfigurationProvider _encryptedConfigurationProvider;
    private readonly Func<string, Task<string>> _decrypt;
    private readonly IDisposable? _disposable;

    public SecretConfigurationProvider(
        IConfigurationProvider encryptedConfigurationProvider,
        Func<string, Task<string>> decrypt)
    {
        _encryptedConfigurationProvider = encryptedConfigurationProvider;
        _decrypt = decrypt;

        _disposable = ChangeToken.OnChange(
            () => encryptedConfigurationProvider.GetReloadToken(),
            async () => await DecryptSource());
    }

    public override void Load()
    {
        _encryptedConfigurationProvider.Load();

        DecryptSource().GetAwaiter().GetResult();
    }

    private async Task DecryptSource()
    {
        Dictionary<string, string> data = new(StringComparer.OrdinalIgnoreCase);
        await DecryptValues(null, data);
        Data = data;
    }

    private async Task DecryptValues(string? prefix, IDictionary<string, string> data)
    {
        IEnumerable<string> childKeys = _encryptedConfigurationProvider
            .GetChildKeys(Enumerable.Empty<string>(), prefix)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (string key in childKeys)
        {
            string surrogateKey = prefix == null ? key : ConfigurationPath.Combine(prefix, key);

            if (_encryptedConfigurationProvider.TryGet(surrogateKey, out string value))
                data.Add(surrogateKey, await _decrypt(value));
            else
                await DecryptValues(surrogateKey, data);
        }
    }

    public override void Set(string key, string value)
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        if (_disposable != null)
            _disposable.Dispose();
    }
}
