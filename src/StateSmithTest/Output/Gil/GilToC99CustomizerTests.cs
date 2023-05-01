#nullable enable

using FluentAssertions;
using StateSmith.Output;
using StateSmith.Output.Gil.C99;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;
using System.Linq;
using Xunit;

namespace StateSmithTest.Output.Gil;

/// <summary>
/// https://github.com/StateSmith/StateSmith/issues/185
/// </summary>
public class GilToC99CustomizerTests
{
    private readonly StateMachine sm = new("MySuperSm");
    private readonly RenderConfigCVars renderConfigC = new();
    private readonly GilToC99Customizer customizer;
    private readonly CapturingCodeFileWriter capturingWriter = new();

    public GilToC99CustomizerTests()
    {
        customizer = new GilToC99Customizer(new StateMachineProvider(sm), renderConfigC);
    }

    [Fact]
    public void GccExample()
    {
        renderConfigC.CEnumDeclarer = "typedef enum __attribute__((packed)) {enumName}";
        customizer.Setup();
        customizer.EnumDeclarationBuilder("SomeEnumType").Should().Be("typedef enum __attribute__((packed)) SomeEnumType");
    }

    [Fact]
    public void SubstitutionVarWhiteSpace()
    {
        renderConfigC.CEnumDeclarer = "some stuff { enumName } __END_STUFF";
        customizer.Setup();
        customizer.EnumDeclarationBuilder("MyEnumType").Should().Be("some stuff MyEnumType __END_STUFF");
    }

    [Fact]
    public void GccIntegrationTest()
    {
        SmRunner runner = new(diagramPath: "ExGil1.drawio", transpilerId: TranspilerId.C99);
        runner.GetExperimentalAccess().DiServiceProvider.AddSingletonT<ICodeFileWriter>(capturingWriter);
        runner.Settings.propagateExceptions = true;
        runner.Run();

        // Note that draw.io file already contains the equivalent of the below
        //class ExampleRenderConfig : IRenderConfigC
        //{
        //    string IRenderConfigC.CEnumDeclarer => "typedef enum __attribute__((packed)) {enumName}";
        //    string IRenderConfigC.CFileExtension => ".cpp";
        //    string IRenderConfigC.HFileExtension => ".hpp";
        //}

        var cppFile = capturingWriter.captures.GetValues(runner.Settings.outputDirectory + "ExGil1.cpp").Single();
        var hppFile = capturingWriter.captures.GetValues(runner.Settings.outputDirectory + "ExGil1.hpp").Single();

        hppFile.code.Should().Contain("typedef enum __attribute__((packed)) ExGil1_StateId");
        hppFile.code.Should().Contain("typedef enum __attribute__((packed)) ExGil1_EventId");
    }
}
