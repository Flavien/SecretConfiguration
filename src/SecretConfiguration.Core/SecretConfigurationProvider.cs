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
    private readonly Func<IDictionary<string, string>, string> _decrypt;
    private readonly string _encryptedRootKey;

    public SecretConfigurationProvider(
        IConfigurationProvider encryptedConfigurationProvider,
        string encryptedRootKey,
        Func<IDictionary<string, string>, string> decrypt)
    {
        _encryptedConfigurationProvider = encryptedConfigurationProvider;
        _encryptedRootKey = encryptedRootKey;
        _decrypt = decrypt;

        encryptedConfigurationProvider.GetReloadToken().RegisterChangeCallback(_ => Load(), null);
    }

    public override void Load()
    {
        _encryptedConfigurationProvider.Load();

        Data.Clear();
        DecryptValues(null);
    }

    private void DecryptValues(string? prefix)
    {
        foreach (string key in _encryptedConfigurationProvider.GetChildKeys(Enumerable.Empty<string>(), prefix))
        {
            string surrogateKey = prefix == null ? key : ConfigurationPath.Combine(prefix, key);
            if (key.Equals(_encryptedRootKey, StringComparison.OrdinalIgnoreCase))
            {
                IDictionary<string, string> properties = new Dictionary<string, string>();
                IEnumerable<string> childKeys = _encryptedConfigurationProvider.GetChildKeys(
                    Enumerable.Empty<string>(),
                    surrogateKey);

                foreach (string propertyKey in childKeys)
                {
                    string propertySurrogateKey = ConfigurationPath.Combine(surrogateKey, propertyKey);
                    if (_encryptedConfigurationProvider.TryGet(propertySurrogateKey, out string value))
                        properties[propertyKey] = value;
                }

                base.Set(prefix, _decrypt(properties));
            }
            else
            {
                DecryptValues(surrogateKey);
            }
        }
    }

    public override void Set(string key, string value)
    {
        throw new NotSupportedException();
    }
}
