using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public static class LevelLoaderPatches
{
	static readonly ModuleDefinition UnityCoreModule = AssemblyDefinition.ReadAssembly("GettingOverIt_Data\\Managed\\UnityEngine.CoreModule.dll").MainModule;
	static readonly ModuleDefinition UnityAudioModule = AssemblyDefinition.ReadAssembly("GettingOverIt_Data\\Managed\\UnityEngine.AudioModule.dll").MainModule;
	static readonly ModuleDefinition UnityAnimationModule = AssemblyDefinition.ReadAssembly("GettingOverIt_Data\\Managed\\UnityEngine.AnimationModule.dll").MainModule;
	static readonly ModuleDefinition UnityPhysics2DModule = AssemblyDefinition.ReadAssembly("GettingOverIt_Data\\Managed\\UnityEngine.Physics2DModule.dll").MainModule;

	public class CustomGroundCol : IClassPatch
	{
		public TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule)
		{

			var MonoBehaviourRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "MonoBehaviour", UnityCoreModule, UnityCoreModule));
			var ColorRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Color", UnityCoreModule, UnityCoreModule, true));

			var CustomGroundCol = new TypeDefinition(
				"", "CustomGroundCol",
				TypeAttributes.Public | TypeAttributes.Class,
				MonoBehaviourRef);

			var groundColField = new FieldDefinition("groundCol", FieldAttributes.Public, ColorRef);
			var materialField = new FieldDefinition("material", FieldAttributes.Public, assemblyModule.TypeSystem.String);
			var isSolidField = new FieldDefinition("isSolid", FieldAttributes.Public, assemblyModule.TypeSystem.Boolean);

			MethodDefinition monoBehaviourCtor = CreateCtor(assemblyModule, MonoBehaviourRef);
			var Awake = new MethodDefinition("Awake", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			Awake.Body.InitLocals = true;
			var Awake_Body = Awake.Body.GetILProcessor();
			Awake_Body.Emit(OpCodes.Ret);

			CustomGroundCol.Fields.Add(groundColField);
			CustomGroundCol.Fields.Add(materialField);
			CustomGroundCol.Fields.Add(isSolidField);
			CustomGroundCol.Methods.Add(monoBehaviourCtor);
			CustomGroundCol.Methods.Add(Awake);
			return CustomGroundCol;
		}
	}
	public class CustomHitSound : IClassPatch
	{
		public TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule)
		{
			var AudioClipRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "AudioClip", UnityAudioModule, UnityAudioModule));
			var GenericListRef = assemblyModule.ImportReference(typeof(List<>));
			var AudioListRef = GenericListRef.MakeGenericInstanceType(AudioClipRef);

			var CustomHitSound = new TypeDefinition(
				"", "CustomHitSound",
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable,
				assemblyModule.TypeSystem.Object);

			var name = new FieldDefinition("name", FieldAttributes.Public, assemblyModule.TypeSystem.String);
			var hits = new FieldDefinition("hits", FieldAttributes.Public, AudioListRef);
			var hardHits = new FieldDefinition("hardHits", FieldAttributes.Public, AudioListRef);
			var scrapes = new FieldDefinition("scrapes", FieldAttributes.Public, AudioListRef);

			MethodDefinition objectCtor = CreateCtor(assemblyModule, assemblyModule.TypeSystem.Object);

			var GetHit = new MethodDefinition("GetHit", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			GetHit.Body.InitLocals = true;
			var GetHit_Body = GetHit.Body.GetILProcessor();
			GetHit_Body.Emit(OpCodes.Ldnull);
			GetHit_Body.Emit(OpCodes.Ret);

			var GetHardHit = new MethodDefinition("GetHardHit", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			GetHardHit.Body.InitLocals = true;
			var GetHardHit_Body = GetHardHit.Body.GetILProcessor();
			GetHardHit_Body.Emit(OpCodes.Ldnull);
			GetHardHit_Body.Emit(OpCodes.Ret);

			var GetScrape = new MethodDefinition("GetScrape", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			GetScrape.Body.InitLocals = true;
			var GetScrape_Body = GetScrape.Body.GetILProcessor();
			GetScrape_Body.Emit(OpCodes.Ldnull);
			GetScrape_Body.Emit(OpCodes.Ret);

			CustomHitSound.Fields.Add(name);
			CustomHitSound.Fields.Add(hits);
			CustomHitSound.Fields.Add(hardHits);
			CustomHitSound.Fields.Add(scrapes);
			CustomHitSound.Methods.Add(objectCtor);
			CustomHitSound.Methods.Add(GetHit);
			CustomHitSound.Methods.Add(GetHardHit);
			CustomHitSound.Methods.Add(GetScrape);
			return CustomHitSound;
		}
	}
	public class CustomHitSoundProvider : IClassPatch
	{
		public TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule)
		{

			var MonoBehaviourRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "MonoBehaviour", UnityCoreModule, UnityCoreModule));
			var AudioClipRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "AudioClip", UnityAudioModule, UnityAudioModule));
			var GenericDictRef = assemblyModule.ImportReference(typeof(Dictionary<,>));

			var CustomHitSoundProvider = new TypeDefinition(
				"", "CustomHitSoundProvider",
				TypeAttributes.Public | TypeAttributes.Class,
				MonoBehaviourRef);

			MethodDefinition monoBehaviourCtor = CreateCtor(assemblyModule, MonoBehaviourRef);

			var Awake = new MethodDefinition("Awake", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			Awake.Body.InitLocals = true;
			var Awake_Body = Awake.Body.GetILProcessor();
			Awake_Body.Emit(OpCodes.Ret);

			var GetHit = new MethodDefinition("GetHit", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			var matParam = new ParameterDefinition("mat", ParameterAttributes.None, assemblyModule.TypeSystem.String);
			GetHit.Parameters.Add(matParam);
			GetHit.Body.InitLocals = true;
			var GetHit_Body = GetHit.Body.GetILProcessor();
			GetHit_Body.Emit(OpCodes.Ldnull);
			GetHit_Body.Emit(OpCodes.Ret);

			var GetHardHit = new MethodDefinition("GetHardHit", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			GetHardHit.Parameters.Add(matParam);
			GetHardHit.Body.InitLocals = true;
			var GetHardHit_Body = GetHardHit.Body.GetILProcessor();
			GetHardHit_Body.Emit(OpCodes.Ldnull);
			GetHardHit_Body.Emit(OpCodes.Ret);

			var GetScrape = new MethodDefinition("GetScrape", MethodAttributes.Public | MethodAttributes.HideBySig, AudioClipRef);
			GetScrape.Parameters.Add(matParam);
			GetScrape.Body.InitLocals = true;
			var GetScrape_Body = GetScrape.Body.GetILProcessor();
			GetScrape_Body.Emit(OpCodes.Ldnull);
			GetScrape_Body.Emit(OpCodes.Ret);

			// will break if CustomHitSound has not been added before this one
			var CustomHitSound = assemblyModule.Types.Last(m => m.Name == "CustomHitSound");
			var hitSounds = new FieldDefinition("hitSounds", FieldAttributes.Public, CustomHitSound.MakeArrayType());
			var hitDict = new FieldDefinition("hitDict", FieldAttributes.Public, GenericDictRef.MakeGenericInstanceType(assemblyModule.TypeSystem.String, CustomHitSound));

			CustomHitSoundProvider.Fields.Add(hitSounds);
			CustomHitSoundProvider.Fields.Add(hitDict);
			CustomHitSoundProvider.Methods.Add(monoBehaviourCtor);
			CustomHitSoundProvider.Methods.Add(Awake);
			CustomHitSoundProvider.Methods.Add(GetHit);
			CustomHitSoundProvider.Methods.Add(GetHardHit);
			CustomHitSoundProvider.Methods.Add(GetScrape);
			return CustomHitSoundProvider;
		}
	}
	public class CustomTrigger : IClassPatch
	{
		public TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule)
		{

			var MonoBehaviourRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "MonoBehaviour", UnityCoreModule, UnityCoreModule));
			var RequireComponentRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "RequireComponent", UnityCoreModule, UnityCoreModule));
			var Collider2DRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Collider2D", UnityPhysics2DModule, UnityPhysics2DModule));
			var Vector3Ref = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Vector3", UnityCoreModule, UnityCoreModule, true));
			var UnityEventRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine.Events", "UnityEvent", UnityCoreModule, UnityCoreModule));
			var AudioClipRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "AudioClip", UnityAudioModule, UnityAudioModule));
			var GameObjectRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "GameObject", UnityCoreModule, UnityCoreModule));
			var GameObjectArrayRef = new ArrayType(GameObjectRef);
			var AudioSourceRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "AudioSource", UnityAudioModule, UnityAudioModule));
			var AnimationRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Animation", UnityAnimationModule, UnityAnimationModule));

			var CustomTrigger = new TypeDefinition(
				"", "CustomTrigger",
				TypeAttributes.Public | TypeAttributes.Class,
				MonoBehaviourRef);

			var RequiredComponentCtor = RequireComponentRef.Resolve().Methods.First(m => m.Parameters.Count == 1);
			var RequiredComponentAttribute = new CustomAttribute(assemblyModule.ImportReference(RequiredComponentCtor));
			RequiredComponentAttribute.ConstructorArguments.Add(new CustomAttributeArgument(assemblyModule.ImportReference(typeof(System.Type)), Collider2DRef));
			CustomTrigger.CustomAttributes.Add(RequiredComponentAttribute);

			var Trigger = new TypeDefinition("", "Trigger", TypeAttributes.NestedPublic | TypeAttributes.Sealed, assemblyModule.ImportReference(typeof(System.Enum)));
			var value__ = new FieldDefinition("value__", FieldAttributes.SpecialName | FieldAttributes.RTSpecialName | FieldAttributes.Public, assemblyModule.TypeSystem.Int32);
			Trigger.Fields.Add(value__);
			var Reset = new FieldDefinition("Reset", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 0 } ;
			Trigger.Fields.Add(Reset);
			var Teleport = new FieldDefinition("Teleport", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 1 } ;
			Trigger.Fields.Add(Teleport);
			var Finish = new FieldDefinition("Finish", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 2 } ;
			Trigger.Fields.Add(Finish);
			var Playsound = new FieldDefinition("Playsound", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 3 } ;
			Trigger.Fields.Add(Playsound);
			var Animation = new FieldDefinition("Animation", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 4 } ;
			Trigger.Fields.Add(Animation);
			var Event = new FieldDefinition("Event", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 5 } ;
			Trigger.Fields.Add(Event);
			var SwitchScene = new FieldDefinition("SwitchScene", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, Trigger) { Constant = 6 } ;
			Trigger.Fields.Add(SwitchScene);

			var DetectMode = new TypeDefinition("", "DetectMode", TypeAttributes.NestedPublic | TypeAttributes.Sealed, assemblyModule.ImportReference(typeof(System.Enum)));
			var value__1 = new FieldDefinition("value__", FieldAttributes.SpecialName | FieldAttributes.RTSpecialName | FieldAttributes.Public, assemblyModule.TypeSystem.Int32);
			DetectMode.Fields.Add(value__1);
			var PlayerOnly = new FieldDefinition("PlayerOnly", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, DetectMode) { Constant = 0 } ;
			DetectMode.Fields.Add(PlayerOnly);
			var PlayerAndProps = new FieldDefinition("PlayerAndProps", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, DetectMode) { Constant = 1 } ;
			DetectMode.Fields.Add(PlayerAndProps);
			var PropsOnly = new FieldDefinition("PropsOnly", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, DetectMode) { Constant = 2 } ;
			DetectMode.Fields.Add(PropsOnly);
			var SpecificProp = new FieldDefinition("SpecificProp", FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault, DetectMode) { Constant = 3 } ;
			DetectMode.Fields.Add(SpecificProp);

			var TriggerType = new FieldDefinition("TriggerType", FieldAttributes.Public, Trigger);
			var Mode = new FieldDefinition("Mode", FieldAttributes.Public, assemblyModule.TypeSystem.Boolean);
			var Destination = new FieldDefinition("Destination", FieldAttributes.Public, Vector3Ref);
			var source = new FieldDefinition("source", FieldAttributes.Public, AudioSourceRef);
			var Sound = new FieldDefinition("Sound", FieldAttributes.Public, AudioClipRef);
			var EnableCredits = new FieldDefinition("EnableCredits", FieldAttributes.Public, assemblyModule.TypeSystem.Boolean);
			var TriggerEvent = new FieldDefinition("TriggerEvent", FieldAttributes.Public, UnityEventRef);
			var animation = new FieldDefinition("animation", FieldAttributes.Public, AnimationRef);
			var anim = new FieldDefinition("anim", FieldAttributes.Public, assemblyModule.TypeSystem.String);
			var Detect = new FieldDefinition("Detect", FieldAttributes.Public, DetectMode);
			var TargetProp = new FieldDefinition("TargetProp", FieldAttributes.Public, GameObjectArrayRef);

			MethodDefinition monoBehaviourCtor = CreateCtor(assemblyModule, MonoBehaviourRef);

			var Start = new MethodDefinition("Start", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			Start.Body.InitLocals = true;
			var Start_Body = Start.Body.GetILProcessor();
			Start_Body.Emit(OpCodes.Ret);

			var OnTriggerEnter2D = new MethodDefinition("OnTriggerEnter2D", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			var colParam = new ParameterDefinition("col", ParameterAttributes.None, Collider2DRef);
			OnTriggerEnter2D.Parameters.Add(colParam);
			OnTriggerEnter2D.Body.InitLocals = true;
			var OnTriggerEnter2D_Body = OnTriggerEnter2D.Body.GetILProcessor();
			OnTriggerEnter2D_Body.Emit(OpCodes.Ret);

			var Reset1 = new MethodDefinition("Reset", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			Reset1.Body.InitLocals = true;
			var Reset1_Body = Reset1.Body.GetILProcessor();
			Reset1_Body.Emit(OpCodes.Ret);

			var OnDrawGizmos = new MethodDefinition("OnDrawGizmos", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			OnDrawGizmos.Body.InitLocals = true;
			var OnDrawGizmos_Body = OnDrawGizmos.Body.GetILProcessor();
			OnDrawGizmos_Body.Emit(OpCodes.Ret);

			CustomTrigger.NestedTypes.Add(Trigger);
			CustomTrigger.NestedTypes.Add(DetectMode);
			CustomTrigger.Fields.Add(TriggerType);
			CustomTrigger.Fields.Add(Mode);
			CustomTrigger.Fields.Add(Destination);
			CustomTrigger.Fields.Add(source);
			CustomTrigger.Fields.Add(Sound);
			CustomTrigger.Fields.Add(EnableCredits);
			CustomTrigger.Fields.Add(TriggerEvent);
			CustomTrigger.Fields.Add(animation);
			CustomTrigger.Fields.Add(anim);
			CustomTrigger.Fields.Add(Detect);
			CustomTrigger.Fields.Add(TargetProp);
			CustomTrigger.Methods.Add(monoBehaviourCtor);
			CustomTrigger.Methods.Add(Start);
			CustomTrigger.Methods.Add(OnTriggerEnter2D);
			CustomTrigger.Methods.Add(Reset1);
			CustomTrigger.Methods.Add(OnDrawGizmos);
			return CustomTrigger;
		}
	}
	public class GravityControlLite : IClassPatch
	{
		public TypeDefinition GetTypeDefinition(ModuleDefinition assemblyModule)
		{

			var MonoBehaviourRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "MonoBehaviour", UnityCoreModule, UnityCoreModule));
			var TransformRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Transform", UnityCoreModule, UnityCoreModule));
			var Collider2DRef = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Collider2D", UnityPhysics2DModule, UnityPhysics2DModule));
			var Vector2Ref = assemblyModule.ImportReference(new TypeReference(
				"UnityEngine", "Vector2", UnityCoreModule, UnityCoreModule, true));


			var GravityControlLite = new TypeDefinition(
				"", "GravityControlLite",
				TypeAttributes.Public | TypeAttributes.Class,
				MonoBehaviourRef);

			// Attractors nested class ===================
			var AttractorsType = new TypeDefinition("", "Attractors", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPublic, assemblyModule.TypeSystem.Object);
			GravityControlLite.NestedTypes.Add(AttractorsType);
			// resolve ctor
			var resolvedCtor = typeof(System.SerializableAttribute).GetConstructor(
				System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
				null,
				new System.Type[0],
				null
			);
			var AttractorsSerializableAttribute = new CustomAttribute(
				assemblyModule.ImportReference(
					resolvedCtor
				)
			);
			AttractorsType.CustomAttributes.Add(AttractorsSerializableAttribute);

			var gravityWellField = new FieldDefinition("gravityWell", FieldAttributes.Public, TransformRef);
			AttractorsType.Fields.Add(gravityWellField);
			var gravityModifierField = new FieldDefinition("gravityModifier", FieldAttributes.Public, assemblyModule.TypeSystem.Single);
			AttractorsType.Fields.Add(gravityModifierField);
			//** Constructor: Attractors() **
			var AttractorsCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, assemblyModule.TypeSystem.Void);
			AttractorsType.Methods.Add(AttractorsCtor);
			var AttractorsCtorBody = AttractorsCtor.Body.GetILProcessor();
			AttractorsCtorBody.Emit(OpCodes.Ldarg_0);
			// get default ctor
			var resolvedAttractorsType = AttractorsType.BaseType.Resolve();
			var AttractorsCtorMethod = resolvedAttractorsType.Methods.SingleOrDefault(m => m.IsConstructor && m.Parameters.Count == 0 && !m.IsStatic);
			var AttractorsCtorMethodReference = new MethodReference(".ctor", AttractorsType.BaseType.Module.TypeSystem.Void, AttractorsType.BaseType) {HasThis = true};
			AttractorsCtorBody.Emit(OpCodes.Call, assemblyModule.ImportReference(AttractorsCtorMethodReference));
			AttractorsCtorBody.Emit(OpCodes.Ret);


			var OnTriggerEnter2D = new MethodDefinition("OnTriggerEnter2D", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			var colParam = new ParameterDefinition("coll", ParameterAttributes.None, Collider2DRef);
			OnTriggerEnter2D.Parameters.Add(colParam);
			OnTriggerEnter2D.Body.InitLocals = true;
			var OnTriggerEnter2D_Body = OnTriggerEnter2D.Body.GetILProcessor();
			OnTriggerEnter2D_Body.Emit(OpCodes.Ret);

			var OnTriggerStay2D = new MethodDefinition("OnTriggerStay2D", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			OnTriggerStay2D.Parameters.Add(colParam);
			OnTriggerStay2D.Body.InitLocals = true;
			var OnTriggerStay2D_Body = OnTriggerStay2D.Body.GetILProcessor();
			OnTriggerStay2D_Body.Emit(OpCodes.Ret);

			var OnTriggerExit2D = new MethodDefinition("OnTriggerExit2D", MethodAttributes.Private | MethodAttributes.HideBySig, assemblyModule.TypeSystem.Void);
			OnTriggerExit2D.Parameters.Add(colParam);
			OnTriggerExit2D.Body.InitLocals = true;
			var OnTriggerExit2D_Body = OnTriggerExit2D.Body.GetILProcessor();
			OnTriggerExit2D_Body.Emit(OpCodes.Ret);

			var gvec = new FieldDefinition("gvec", FieldAttributes.Private, Vector2Ref);
			var gravityWells = new FieldDefinition("gravityWells", FieldAttributes.Public, AttractorsType.MakeArrayType());
			var gravmod = new FieldDefinition("gravmod", FieldAttributes.Public | FieldAttributes.Static, assemblyModule.TypeSystem.Single);

			MethodDefinition monoBehaviourCtor2 = CreateCtor(assemblyModule, MonoBehaviourRef);

			GravityControlLite.Fields.Add(gvec);
			GravityControlLite.Fields.Add(gravityWells);
			GravityControlLite.Fields.Add(gravmod);
			GravityControlLite.Methods.Add(monoBehaviourCtor2);
			GravityControlLite.Methods.Add(OnTriggerEnter2D);
			GravityControlLite.Methods.Add(OnTriggerStay2D);
			GravityControlLite.Methods.Add(OnTriggerExit2D);
			return GravityControlLite;
		}
	}

	private static MethodDefinition CreateCtor(ModuleDefinition module, TypeReference inheritedTypeRef)
	{
		var ctor = new MethodDefinition(".ctor",
			MethodAttributes.Public |
			MethodAttributes.HideBySig |
			MethodAttributes.SpecialName |
			MethodAttributes.RTSpecialName,
			module.TypeSystem.Void);

		var il = ctor.Body.GetILProcessor();

		// base .ctor()
		il.Append(il.Create(OpCodes.Ldarg_0));
		il.Append(il.Create(OpCodes.Call, module.ImportReference(
			new MethodReference(".ctor", module.TypeSystem.Void, inheritedTypeRef) { HasThis = true })));
		il.Append(il.Create(OpCodes.Ret));
		return ctor;
	}
}
