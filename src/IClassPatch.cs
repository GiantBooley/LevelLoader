using Mono.Cecil;

public interface IClassPatch
{
    TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule);
}