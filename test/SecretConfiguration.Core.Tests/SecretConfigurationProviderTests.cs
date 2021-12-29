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

namespace SecretConfiguration.Core.Tests;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

public class SecretConfigurationProviderTests
{
    private readonly MemoryConfigurationProvider _sourceConfiguration = new(new MemoryConfigurationSource());
    private static readonly Func<string, Task<string>> _transform = (x) => Task.FromResult(x + "_decrypted");

    [Fact]
    public void DecryptValues_NestedKeys()
    {
        _sourceConfiguration.Set("node1:child1", "value1");
        _sourceConfiguration.Set("node2:child1", "value2");
        _sourceConfiguration.Set("node2:child2", "value3");
        _sourceConfiguration.Set("node2:child3:child1", "value4");
        _sourceConfiguration.Set("node3", "value5");
        _sourceConfiguration.Set("node4:0", "value6");
        _sourceConfiguration.Set("node4:1", "value7");

        SecretConfigurationProvider provider = new(_sourceConfiguration, _transform);
        provider.Load();

        ConfigurationRoot root = new(new IConfigurationProvider[] { provider });

        const string expected =
            @"node1:
                child1=value1_decrypted (SecretConfigurationProvider)
              node2:
                child1=value2_decrypted (SecretConfigurationProvider)
                child2=value3_decrypted (SecretConfigurationProvider)
                child3:
                  child1=value4_decrypted (SecretConfigurationProvider)
              node3=value5_decrypted (SecretConfigurationProvider)
              node4:
                0=value6_decrypted (SecretConfigurationProvider)
                1=value7_decrypted (SecretConfigurationProvider)";

        Assert.Equal(CollapseWhiteSpaces(expected), CollapseWhiteSpaces(root.GetDebugView()));
    }

    [Fact]
    public void Set_Throws()
    {
        SecretConfigurationProvider provider = new(_sourceConfiguration, _transform);
        Assert.Throws<NotSupportedException>(
            () => provider.Set("key", "value"));
    }

    private static string CollapseWhiteSpaces(string input) => Regex.Replace(input, "\\s+", _ => " ").Trim();
}
