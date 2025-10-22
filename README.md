See the original README in the original Repo : https://github.com/MessagePack-CSharp/MessagePack-CSharp

### This fork adapts MessagePack-CSharp to deal with circular references.

Right now, it has only one commit on top of the original repo.

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