// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

#if DYNAMIC_GENERATION

using System.Collections.Generic;
using MessagePack.Resolvers;

namespace MessagePack.Tests
{
    public class CircularReferencesTest
    {
        [MessagePackObject, AllowCircularRefrerences]
        public class CircularRef
        {
            [Key(0)]
            public CircularRef X;

            [Key(1)]
            public CircularRef Y;

            [Key(2)]
            public List<CircularRef> Z;

            [Key(3)]
            public CircularRef I;
        }

        [MessagePackObject, AllowCircularRefrerences]
        public class ParentRef
        {
            [Key(0)]
            public string PropParent;

            [Key(1)]
            public List<ChildRef> Children;
        }

        [MessagePackObject, AllowCircularRefrerences]
        public class ChildRef
        {
            [Key(0)]
            public string PropChild;

            [Key(1)]
            public ParentRef Parent;
        }

        [Fact]
        public void CircularRefTest()
        {
            var cirRef = new CircularRef();
            cirRef.X = cirRef;
            cirRef.Y = new CircularRef();
            cirRef.Z = new List<CircularRef>() { cirRef.Y, cirRef, cirRef.X, cirRef.Y, new CircularRef() };
            cirRef.I = cirRef.Z[4];
            cirRef.I.X = cirRef;
            var x = MessagePackSerializer.Serialize(cirRef, MessagePackSerializerOptions.Standard.WithResolver(new CircularReferencesResolver(MessagePackSerializerOptions.Standard.Resolver)));
            var r = MessagePackSerializer.Deserialize<CircularRef>(x, MessagePackSerializerOptions.Standard.WithResolver(new CircularReferencesResolver(MessagePackSerializerOptions.Standard.Resolver)));
            r.IsSameReferenceAs(r.X);
            r.IsSameReferenceAs(r.Z[1]);
            r.IsSameReferenceAs(r.Z[2]);
            r.Y.IsSameReferenceAs(r.Z[0]);
            r.Y.IsSameReferenceAs(r.Z[3]);
            r.I.IsSameReferenceAs(r.Z[4]);
            r.I.X.IsSameReferenceAs(r);
        }

        [Fact]
        public void ParentChildrenTest()
        {
            var parent = new ParentRef
            {
                PropParent = "this is the parent",
                Children = [],
            };

            parent.Children.Add(new ChildRef()
            {
                PropChild = "this is the first child",
                Parent = parent,
            });

            parent.Children.Add(new ChildRef()
            {
                PropChild = "this is the second child",
                Parent = parent,
            });

            var x = MessagePackSerializer.Serialize(parent, MessagePackSerializerOptions.Standard.WithResolver(new CircularReferencesResolver(MessagePackSerializerOptions.Standard.Resolver)));
            var r = MessagePackSerializer.Deserialize<ParentRef>(x, MessagePackSerializerOptions.Standard.WithResolver(new CircularReferencesResolver(MessagePackSerializerOptions.Standard.Resolver)));

            r.IsSameReferenceAs(r.Children[0].Parent);
            r.IsSameReferenceAs(r.Children[1].Parent);
        }
    }
}

#endif
