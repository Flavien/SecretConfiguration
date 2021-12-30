# SecretConfiguration.AwsKms

[![SecretConfiguration.AwsKms](https://img.shields.io/nuget/v/SecretConfiguration.AwsKms.svg?style=flat-square&color=blue&logo=nuget)](https://www.nuget.org/packages/SecretConfiguration.AwsKms/)

`SecretConfiguration.AwsKms` is a third-party configuration provider compatible with the `Microsoft.Extensions.Configuration` package. It facilitates the storage of secrets in encrypted form in configuration files.

Encryption and decryption is performed using [AWS Key Management Service](https://aws.amazon.com/kms/).

## Why use this?

Application configuration, including secrets, are sometimes managed externally from outside the application, with solutions such as Hashicorp Vault or AWS Systems Manager Parameter Store.

The drawback of this approach is that the set of settings required by an application evolve as new versions of the application are released. This means a separate version control system has to be introduced, on top of the version control system already used for the application source code. This also increases the potential for error as two closely related systems have to be kept in sync at all times.

Storing configuration in source control along with the code that relies on it allows configuration and code to be versioned together.

However, secrets may never be stored in clear text. The `SecretConfiguration.AwsKms` package makes it possible to store secrets in a repository in encrypted form, and decrypts them transparently at runtime.

The cryptographic material is managed by AWS through the KMS service. Encryption and decryption of secrets is managed by this service.

## Setup

### Create a key on AWS KMS

First create a key on AWS KMS, either using the [console](https://eu-west-1.console.aws.amazon.com/kms/home), or the command line.

It is recommended to use symmetric encryption as this will allow to encrypt values of up to 4kb with the default key spec.

Take note of the KMS ARN once the key is created, for example: `arn:aws:kms:eu-west-1:123456789:key/11111111-0000-0000-0000-000000000000`.

### Encrypt a secret

Use the following command from the [AWS CLI](https://aws.amazon.com/cli/) to create the ciphertext for the secret:

```bash
aws kms encrypt --cli-binary-format raw-in-base64-out --key-id "11111111-0000-0000-0000-000000000000" --plaintext "SECRET_TO_ENCRYPT"
```

The `key-id` parameter should be replaced by the actual KMS key ID obtained in the previous step.

The output will look like:

```json
{
    "CiphertextBlob": "AQICAHhDR/VQh6Ap...rfyKsKCG2h6WVK8=",
    "KeyId": "arn:aws:kms:eu-west-1:123456789:key/11111111-0000-0000-0000-000000000000",
    "EncryptionAlgorithm": "SYMMETRIC_DEFAULT"
}
```

The `CiphertextBlob` property in the response is the value that will be added to the encrypted configuration file.

### Create a encrypted configuration file

Create a `secrets.json` file. This file will be used to store encrypted secrets. For example, add this content:

```json
{
  "Database": {
    "Password": "AQICAHhDR/VQh6Ap...rfyKsKCG2h6WVK8="
  }
}
```

Replace the value with the ciphertext value obtained in the previous step.

### Register the configuration provider

```csharp
string keyId = "arn:aws:kms:eu-west-1:123456789:key/11111111-0000-0000-0000-000000000000";

builder.Configuration.AddAwsKmsEncryptedConfiguration(
  new AmazonKeyManagementServiceClient(),
  keyId,
  encryptedSource => encryptedSource
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("secrets.json"));
```

This allows the encrypted configuration file (`secrets.json`) to be decrypted during startup using the KMS service. The decrypted configuration settings are then merged with the rest of the configuration obtained from other sources (such as command line, environment variables, or clear text JSON configuration files).

### Use encrypted configuration settings

The configuration keys provided through the `KmsSecretConfigurationSource` are now available in their decrypted form throughout the application:

```csharp
IConfiguration configuration;

string databasePassword = configuration["Database:Password"];
```

## License

Copyright 2021 Flavien Charlon

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
