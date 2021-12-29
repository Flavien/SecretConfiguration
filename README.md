# SecretConfiguration.Kms

`SecretConfiguration.Kms` is a configuration provider compatible with the `Microsoft.Extensions.Configuration` package, that facilitates the storage of secrets in encrypted form in configuration files.

Encryption and decryption is performed using AWS Key Management Service.

## Why use this?

Some solutions 

Storing configuration in source control along with the code that uses them allows configuration and code to be versioned together.

Since secrets may never be stored in clear text, this solution makes it possible to store secrets in a repository without compromising their security.

## Setup

### Create a key on AWS KMS

First create a key on AWS KMS, either using the [console](https://eu-west-1.console.aws.amazon.com/kms/home), or the command line.

It is recommended to use symmetric encryption as this will allow you to encrypt values of up to 4kb with the default key spec.

Take note of the KMS ARN once the key is created, for example: `arn:aws:kms:eu-west-1:123456789:key/11111111-0000-0000-0000-000000000000`.

### Encrypt a secret

Use the following command from the AWS CLI to create the ciphertext for the secret:

```bash
aws kms encrypt --cli-binary-format raw-in-base64-out --key-id "11111111-0000-0000-0000-000000000000" --plaintext "SECRET_TO_ENCRYPT"
```

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

builder.Configuration
    .AddKmsEncryptedSecretFile(
        new AmazonKeyManagementServiceClient(),
        keyId,
        encryptedSource => encryptedSource
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("secrets.json"));
````

### Use encrypted configuration settings



## License

Copyright 2021 Flavien Charlon

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
