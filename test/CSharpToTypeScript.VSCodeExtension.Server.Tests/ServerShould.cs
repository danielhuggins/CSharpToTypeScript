using Xunit;
using CSharpToTypeScript.Server;
using CSharpToTypeScript.Core.Services;
using CSharpToTypeScript.Core.Options;
using Moq;
using Server.Services;
using Newtonsoft.Json;
using CSharpToTypeScript.Server.DTOs;
using Newtonsoft.Json.Serialization;

namespace CSharpToTypeScript.VSCodeExtension.Server.Tests
{
    public class ServerShould
    {
        [Fact]
        public void HandleRequests()
        {
            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            var firstRequest = JsonConvert.SerializeObject(
                new Input { Code = "class First { }", Export = true, UseTabs = false, TabSize = 4 },
                serializerSettings);

            var secondRequest = JsonConvert.SerializeObject(
                new Input { Code = "class Second { }", Export = true, UseTabs = false, TabSize = 2 },
                serializerSettings);

            var stdioMock = new Mock<IStdio>();
            stdioMock.SetupSequence(s => s.ReadLine())
                .Returns(firstRequest)
                .Returns(secondRequest)
                .Returns("EXIT");

            var codeConverterMock = new Mock<ICodeConverter>();
            codeConverterMock.SetupSequence(c => c.ConvertToTypeScript(
                    It.IsAny<string>(), It.IsAny<CodeConversionOptions>()))
                .Returns("export interface First { }")
                .Returns("export interface Second { }");

            var server = new StdioServer(codeConverterMock.Object, stdioMock.Object);

            server.Handle();

            stdioMock.Verify(s => s.ReadLine(), Times.Exactly(3));

            stdioMock.Verify(s =>
                s.WriteLine(It.Is<string>(
                    response => JsonConvert.DeserializeObject<Output>(response).ConvertedCode == "export interface First { }")),
                Times.Once);

            stdioMock.Verify(s => s.WriteLine(It.IsAny<string>()), Times.Exactly(2));

            codeConverterMock.Verify(c =>
                c.ConvertToTypeScript("class Second { }", It.Is<CodeConversionOptions>(options => options.TabSize == 2)),
                Times.Once);

            codeConverterMock.Verify(c =>
                c.ConvertToTypeScript(It.IsAny<string>(), It.IsAny<CodeConversionOptions>()),
                Times.Exactly(2));
        }
    }
}