using System.Collections.Generic;
using Mono.Cecil;

public static class LevelLoaderPatcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};

    public static void Patch(ref AssemblyDefinition assembly)
    {
        var assemblyModule = assembly.MainModule;
        assemblyModule.Types.Add(new LevelLoaderPatches.CustomGroundCol().GetTypeDefinition(assemblyModule));
        assemblyModule.Types.Add(new LevelLoaderPatches.CustomHitSound().GetTypeDefinition(assemblyModule));
        assemblyModule.Types.Add(new LevelLoaderPatches.CustomHitSoundProvider().GetTypeDefinition(assemblyModule));
        assemblyModule.Types.Add(new LevelLoaderPatches.CustomTrigger().GetTypeDefinition(assemblyModule));
        assemblyModule.Types.Add(new LevelLoaderPatches.GravityControlLite().GetTypeDefinition(assemblyModule));
    }
}
