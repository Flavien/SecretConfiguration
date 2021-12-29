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

using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Configuration;
using SecretConfiguration.Core;

public class KmsSecretConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider EncryptedConfigurationProvider { get; set; } = null!;

    public AmazonKeyManagementServiceClient KmsClient { get; set; } = null!;

    public string KmsKeyId { get; set; } = null!;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        AmazonKeyManagementServiceClient kmsClient = KmsClient;
        string kmsKeyId = KmsKeyId;

        SecretConfigurationSource configurationSource = new()
        {
            EncryptedConfigurationProvider = EncryptedConfigurationProvider,
            Decrypt = ciphertext => KmsDecrypt(kmsClient, kmsKeyId, ciphertext),
        };

        return configurationSource.Build(builder);
    }

    private static async Task<string> KmsDecrypt(
        AmazonKeyManagementServiceClient kmsClient,
        string kmsKeyId,
        string ciphertext)
    {
        using MemoryStream ciphertextBlob = new(Convert.FromBase64String(ciphertext));

        DecryptRequest decryptRequest = new()
        {
            CiphertextBlob = ciphertextBlob,
            KeyId = kmsKeyId
        };

        DecryptResponse plainText = await kmsClient.DecryptAsync(decryptRequest);
        using StreamReader reader = new(plainText.Plaintext);
        return await reader.ReadToEndAsync();
    }
}
