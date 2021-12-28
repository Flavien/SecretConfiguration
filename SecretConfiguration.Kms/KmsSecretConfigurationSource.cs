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

namespace SecretConfiguration.Kms;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Configuration;
using SecretConfiguration.Core;

public class KmsSecretConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider EncryptedConfigurationProvider { get; set; } = null!;

    public string EncryptedRootKey { get; set; } = "value";

    public AmazonKeyManagementServiceClient KmsClient { get; set; } = null!;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        SecretConfigurationSource configurationSource = new SecretConfigurationSource()
        {
            EncryptedConfigurationProvider = EncryptedConfigurationProvider,
            EncryptedRootKey = EncryptedRootKey,
            Decrypt = properties => KmsDecrypt(properties).ConfigureAwait(false).GetAwaiter().GetResult()
        };

        return configurationSource.Build(builder);
    }

    private async Task<string> KmsDecrypt(IDictionary<string, string> properties)
    {
        string keyId = properties["KeyId"];

        using MemoryStream ciphertextBlob = new(Encoding.UTF8.GetBytes(properties["Ciphertext"]));

        DecryptRequest decryptRequest = new()
        {
            CiphertextBlob = ciphertextBlob,
            KeyId = keyId
        };

        using MemoryStream plainText = (await KmsClient.DecryptAsync(decryptRequest)).Plaintext;

        return Encoding.UTF8.GetString(plainText.GetBuffer());
    }
}
