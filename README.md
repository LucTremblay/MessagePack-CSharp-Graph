See the original README in the original Repo : https://github.com/MessagePack-CSharp/MessagePack-CSharp

### This fork adapts MessagePack-CSharp to deal with circular references.

## New attempt (using a source generator)

Full source of the generator can be found here (it's the only thing you will need) :
https://gist.github.com/LucTremblay/a9317528b131588718a1ba99d8720dc5

It use `DataContractAttribute` and `DataMemberAttribute` to find the types and properties to serialize.

`KnownTypeAttribute` is supported.

Circular references are supported by default.

Source can be easily adapted to your needs.

### Steps to use the generator :

#### 1.

- Create a new cs project targeting .net standard 2.0
- Add the Microsoft.CodeAnalysis.Analyzers nuget package
- Add the Microsoft.CodeAnalysis.CSharp nuget package
- Add the MessagePack nuget package

.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>  
  
  <ItemGroup>
    <PackageReference Include="MessagePack" Version="3.1.4" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
  </ItemGroup>

</Project>

```

#### 2.
- Add the full source in a new file
  https://gist.github.com/LucTremblay/a9317528b131588718a1ba99d8720dc5

#### 3.
- In the project containing the types you want to serialize, add a reference to the generator project.
  (my generator project was named `ClassLibrary_SourceGen`). The ReferenceOutputAssembly property is set to true because it contains the new `MessagePackGraphSerializerOptions` class that is used to call the `.GraphOptions()` extension method.
```xml
    <ProjectReference Include="..\ClassLibrary_SourceGen\ClassLibrary_SourceGen.csproj">
      <Project>{6ee58fb4-4300-49b4-9c5a-ef5c0b2c2e5b}</Project>
      <Name>ClassLibrary_SourceGen</Name>
      <OutputItemType>Analyzer</OutputItemType>
      <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
    </ProjectReference>
```

  Generator use `DataContractAttribute` to find the types to generate formatters for.
  
  The `DataMemberAttribute` is used to find the properties of the type to serialize.
   
  The `KnownTypeAttribute` is supported.

- Also add the MessagePack nuget package to the project containing the types you want to serialize.

- When using message pack you must call the `.GraphOptions()` extension on your message pack options. A new call to '.GraphOptions()' must be made for every 'Serialize/Deserialize' call like so:
  ```csharp
    var bytes = MessagePackSerializer.Serialize(obj, options.GraphOptions());
    var obj = MessagePackSerializer.Deserialize<MyType>(bytes, options.GraphOptions());"");
  ```
  The reason for this is to support circular references and object references are stored in the options, so we need to have a new cleared options for every serialization/deserialization process.

## Initial attempt (no so good)

The changes are based on this `DedupingResolver` https://gist.github.com/AArnott/099d5b4d559cbcca2c1c2b0bd61aa951.

- `CircularReferencesResolver` :
  
  Create new `CircularReferencesFormatter<T>` for types mark with the `AllowCircularRefrerencesAttribute`

  Hold the dictionary of serialized object for the serialization process.

- `CircularReferencesFormatter<T>` :

  It wraps an inner formatter, and add serialized object to a dictionary.

  If the object has already been serialized, it writes an ID for the object as an extension.
  
  For the deserialization process, things have to be different. We must keep track of the deserialized a object as soon as it is created, before we continue with deserializing any properties of this object because a property of this object could be a reference to itself.

  The object creation happens in the generated formatters created by the `DynamicObjectResolver`.
  
  Those generated formatters will add the deserialized objects to an array using the `ObjectReferencesHelper` as soon as it is created.

  Finally, during deserialization,  when an extension is found in the reader, the ID can be used to find the object in the deserialized objects array using the `ObjectReferencesHelper`. 

- `AllowCircularRefrerencesAttribute` : 

  This attribute controls what type of object will be tracked in the serialization dictionary/deserialization array. This is useful for two reason. <br>
  1. To avoid impact on performance, it keeps the serialization dictionary/deserialization array with a lower count of object. <br>
  1. Since the serialization dictionary and deserialization array are dealt with differently, it simplifies the tracking by insuring only the same objects are added to them. 