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
using Amazon.KeyManagementService;
using Microsoft.Extensions.Configuration;

public static class SecretConfigurationExtensions
{
    public static IConfigurationBuilder AddKmsEncryptedSecretFile(
        this IConfigurationBuilder builder,
        AmazonKeyManagementServiceClient kmsClient,
        Action<ConfigurationBuilder> configureEncryptedSource)
    {
        ConfigurationBuilder encryptedSourceBuilder = new();
        configureEncryptedSource(encryptedSourceBuilder);

        ChainedConfigurationProvider configurationProvider = new(
            new ChainedConfigurationSource()
            {
                Configuration = encryptedSourceBuilder.Build()
            });

        return builder.Add(
            new KmsSecretConfigurationSource()
            {
                EncryptedConfigurationProvider = configurationProvider,
                KmsClient = kmsClient
            });
    }
}
