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
using Microsoft.Extensions.Configuration;

public class SecretConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationProvider _encryptedConfigurationProvider;
    private readonly Func<string, string> _decrypt;

    public SecretConfigurationProvider(
        IConfigurationProvider encryptedConfigurationProvider,
        Func<string, string> decrypt)
    {
        _encryptedConfigurationProvider = encryptedConfigurationProvider;
        _decrypt = decrypt;

        encryptedConfigurationProvider.GetReloadToken().RegisterChangeCallback(_ => Load(), null);
    }

    public override void Load()
    {
        _encryptedConfigurationProvider.Load();

        Dictionary<string, string> data = new(StringComparer.OrdinalIgnoreCase);
        DecryptValues(null, data);
        Data = data;
    }

    private void DecryptValues(string? prefix, IDictionary<string, string> data)
    {
        foreach (string key in _encryptedConfigurationProvider.GetChildKeys(Enumerable.Empty<string>(), prefix))
        {
            string surrogateKey = prefix == null ? key : ConfigurationPath.Combine(prefix, key);

            if (_encryptedConfigurationProvider.TryGet(surrogateKey, out string value))
                data.Add(surrogateKey, _decrypt(value));
            else
                DecryptValues(surrogateKey, data);
        }
    }

    public override void Set(string key, string value)
    {
        throw new NotSupportedException();
    }
}
