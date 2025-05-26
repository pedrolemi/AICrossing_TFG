using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

// Clase que permite importar archivos .gbnf, donde se define una gramatica
// de un modelo, como un archivo de texto normal

[ScriptedImporter(1, ".gbnf")]
public class GBNFImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        TextAsset asset = new TextAsset(File.ReadAllText(ctx.assetPath));
        ctx.AddObjectToAsset("text", asset);
        ctx.SetMainObject(asset);
    }
}
