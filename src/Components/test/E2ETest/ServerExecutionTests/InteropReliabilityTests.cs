using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    public class InteropReliabilityTests : ServerTestBase<AspNetSiteServerFixture>
    {
        private const int DefaultLatencyTimeout = 500;

        public InteropReliabilityTests(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            _serverFixture.Environment = AspNetEnvironment.Development;
            _serverFixture.BuildWebHostMethod = ComponentsApp.Server.Program.BuildWebHost;
        }

        [Fact]
        public async Task CannotInvokeNonJSInvokableMethods()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.ArgumentException: The assembly \\u0027System.IO.FileSystem\\u0027 does not contain a public method with [JSInvokableAttribute(\\u0022WriteAllText\\u0022)].\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.GetCachedMethodInfo(AssemblyKey assemblyKey, String methodIdentifier)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "System.IO.FileSystem",
                "WriteAllText",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeNonExistingMethods()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.ArgumentException: The assembly \\u0027ComponentsApp.Server\\u0027 does not contain a public method with [JSInvokableAttribute(\\u0022MadeUpMethod\\u0022)].\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.GetCachedMethodInfo(AssemblyKey assemblyKey, String methodIdentifier)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "MadeUpMethod",
                null,
                JsonSerializer.Serialize(new[] { ".\\log.txt", "log" }));

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongNumberOfArguments()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.ArgumentException: In call to \\u0027NotifyLocationChanged\\u0027, expected 2 parameters but received 1.\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.ParseArguments(String methodIdentifier, String argsJson, Type[] parameterTypes)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new[] { _serverFixture.RootUri }));

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyAssemblyName()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.ArgumentException: Cannot be null, empty, or whitespace. (Parameter \\u0027AssemblyName\\u0027)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.GetCachedMethodInfo(AssemblyKey assemblyKey, String methodIdentifier)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsEmptyMethodName()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.ArgumentException: Cannot be null, empty, or whitespace. (Parameter \\u0027methodIdentifier\\u0027)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.GetCachedMethodInfo(AssemblyKey assemblyKey, String methodIdentifier)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithWrongReferenceId()
        {
            // Arrange
            var expectedDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "CreateInformation",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedDotNetObjectRef);
            });

            client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                1,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", "[\"1\",true,\"egasseM\"]");
            });

            client.InvokeDotNetMethod(
                "1",
                null,
                "Reverse",
                3, // non existing ref
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(5000);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", "[\"1\",true,\"egasseM\"]");
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWronReferenceIdType()
        {
            // Arrange
            var expectedImportantDotNetObjectRef = "[\"1\",true,{\"__dotNetObject\":1}]";
            var expectedError = "[\"1\",false,\"System.InvalidCastException: Unable to cast object of type \\u0027ComponentsApp.Server.ImportantInformation\\u0027 to type \\u0027ComponentsApp.Server.TrivialInformation\\u0027.\\r\\n   at Microsoft.JSInterop.DotNetObjectRef\\u00601.set___dotNetObject(Int64 value)\\r\\n   at System.Text.Json.JsonPropertyInfoNotNullable\\u00604.OnRead(JsonTokenType tokenType, ReadStack\\u0026 state, Utf8JsonReader\\u0026 reader)\\r\\n   at System.Text.Json.JsonPropertyInfo.Read(JsonTokenType tokenType, ReadStack\\u0026 state, Utf8JsonReader\\u0026 reader)\\r\\n   at System.Text.Json.JsonSerializer.HandleValue(JsonTokenType tokenType, JsonSerializerOptions options, Utf8JsonReader\\u0026 reader, ReadStack\\u0026 state)\\r\\n   at System.Text.Json.JsonSerializer.ReadCore(JsonSerializerOptions options, Utf8JsonReader\\u0026 reader, ReadStack\\u0026 readStack)\\r\\n   at System.Text.Json.JsonSerializer.ReadCore(Type returnType, JsonSerializerOptions options, Utf8JsonReader\\u0026 reader)\\r\\n   at System.Text.Json.JsonSerializer.ParseCore(String json, Type returnType, JsonSerializerOptions options)\\r\\n   at System.Text.Json.JsonSerializer.Deserialize(String json, Type returnType, JsonSerializerOptions options)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.ParseArguments(String methodIdentifier, String argsJson, Type[] parameterTypes)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "CreateImportant",
                null,
                JsonSerializer.Serialize(Array.Empty<object>()));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedImportantDotNetObjectRef);
            });

            client.InvokeDotNetMethod(
                "1",
                "ComponentsApp.Server",
                "ReceiveTrivial",
                null,
                JsonSerializer.Serialize(new object[] { new { __dotNetObject = 1 } }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element == (0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", expectedError);
            });

            await ValidateClientKeepsWorking(client, batches);
        }

        [Fact]
        public async Task CannotInvokeJSInvokableMethodsWithInvalidArgumentsPayload()
        {
            // Arrange
            var expectedError = "[\"1\",false,\"System.Text.Json.JsonReaderException: \\u0027}\\u0027 is invalid without a matching open. LineNumber: 0 | BytePositionInLine: 18.\\r\\n   at System.Text.Json.ThrowHelper.ThrowJsonReaderException(Utf8JsonReader\\u0026 json, ExceptionResource resource, Byte nextByte, ReadOnlySpan\\u00601 bytes)\\r\\n   at System.Text.Json.Utf8JsonReader.EndObject()\\r\\n   at System.Text.Json.Utf8JsonReader.ConsumeNextToken(Byte marker)\\r\\n   at System.Text.Json.Utf8JsonReader.ConsumeNextTokenOrRollback(Byte marker)\\r\\n   at System.Text.Json.Utf8JsonReader.ReadSingleSegment()\\r\\n   at System.Text.Json.Utf8JsonReader.Read()\\r\\n   at System.Text.Json.JsonDocument.Parse(ReadOnlySpan\\u00601 utf8JsonSpan, Utf8JsonReader reader, MetadataDb\\u0026 database, StackRowStack\\u0026 stack)\\r\\n   at System.Text.Json.JsonDocument.Parse(ReadOnlyMemory\\u00601 utf8Json, JsonReaderOptions readerOptions, Byte[] extraRentedBytes)\\r\\n   at System.Text.Json.JsonDocument.Parse(ReadOnlyMemory\\u00601 json, JsonDocumentOptions options)\\r\\n   at System.Text.Json.JsonDocument.Parse(String json, JsonDocumentOptions options)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.ParseArguments(String methodIdentifier, String argsJson, Type[] parameterTypes)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.InvokeSynchronously(String assemblyName, String methodIdentifier, Object targetInstance, String argsJson)\\r\\n   at Microsoft.JSInterop.DotNetDispatcher.BeginInvoke(String callId, String assemblyName, String methodIdentifier, Int64 dotNetObjectId, String argsJson)\"]";
            var client = new BlazorClient();
            var interopCalls = new List<(int, string, string)>();
            client.JSInterop += CaptureInterop;
            var batches = new List<(int, int, byte[])>();
            client.RenderBatchReceived += (id, renderer, data) => batches.Add((id, renderer, data));

            void CaptureInterop(int arg1, string arg2, string arg3)
            {
                interopCalls.Add((arg1, arg2, arg3));
            }

            Assert.True(await client.ConnectAsync(_serverFixture.RootUri, prerendered: true), "Couldn't connect to the app");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Single(batches);

            // Assert
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                "[ \"invalidPayload\"}");

            await Task.Delay(1000);
            var (callId, functionName, arguments) = Assert.Single(interopCalls, ((int callId, string functionName, string arguments) element) =>
            {
                return element.callId == 0 && element.functionName == "DotNet.jsCallDispatcher.endInvokeDotNetFromJS";
            });

            Assert.Equal(expectedError, arguments);

            await ValidateClientKeepsWorking(client, batches);
        }

        private async Task ValidateClientKeepsWorking(BlazorClient client, List<(int, int, byte[])> batches)
        {
            client.InvokeDotNetMethod(
                "1",
                "Microsoft.AspNetCore.Components.Server",
                "NotifyLocationChanged",
                null,
                JsonSerializer.Serialize(new object[] { _serverFixture.RootUri + "counter", false }));

            await Task.Delay(DefaultLatencyTimeout);
            Assert.Equal(4, batches.Count);

            await client.ClickAsync("thecounter");
            await Task.Delay(DefaultLatencyTimeout);
            Assert.Equal(5, batches.Count);
        }
    }
}
