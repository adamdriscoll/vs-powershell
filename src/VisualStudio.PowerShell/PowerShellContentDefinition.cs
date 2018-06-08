using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace VisualStudio.PowerShell
{
    public class PowerShellContentDefinition
    {
        [Export]
        [Name("PowerShell")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition BarContentTypeDefinition;


        [Export]
        [FileExtension(".ps1")]
        [ContentType("PowerShell1")]
        internal static FileExtensionToContentTypeDefinition BarFileExtensionDefinition;
    }
}
