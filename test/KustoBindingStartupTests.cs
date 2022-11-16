// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoBindingStartupTests
    {
        [Fact]
        public void StartupIsDiscoverable()
        {
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder.UseExternalStartup(new TestStartupTypeLocator());
                })
                .Build();

            IExtensionConfigProvider extensionConfig = host.Services.GetServices<IExtensionConfigProvider>().Single();
            Assert.NotNull(extensionConfig);
            Assert.IsType<KustoExtensionConfigProvider>(extensionConfig);
        }

        private class TestStartupTypeLocator : IWebJobsStartupTypeLocator
        {
            public Type[] GetStartupTypes()
            {
                WebJobsStartupAttribute startupAttribute = typeof(KustoContext).Assembly
                    .GetCustomAttributes<WebJobsStartupAttribute>().Single();

                return new[] { startupAttribute.WebJobsStartupType };
            }
        }
    }
}