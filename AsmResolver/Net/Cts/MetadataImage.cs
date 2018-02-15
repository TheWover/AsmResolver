﻿using System;
using System.Collections.Generic;
using AsmResolver.Net.Metadata;

namespace AsmResolver.Net.Cts
{
    public class MetadataImage
    {
        private readonly IDictionary<MetadataToken, IMetadataMember> _cachedMembers = new Dictionary<MetadataToken, IMetadataMember>();

        internal MetadataImage(MetadataHeader header)
        {
            if (header == null)
                throw new ArgumentNullException("header");
            Header = header;
            var tableStream = header.GetStream<TableStream>();

            var table = tableStream.GetTable(MetadataTokenType.Assembly);
            Assembly = (AssemblyDefinition) table.GetMemberFromRow(this, table.GetRow(0));

            TypeSystem = new TypeSystem(this, Assembly.Name == "mscorlib");
        }

        public MetadataHeader Header
        {
            get;
            private set;
        }

        public TypeSystem TypeSystem
        {
            get;
            private set;
        }

        public AssemblyDefinition Assembly
        {
            get;
            private set;
        }

        internal bool TryGetCachedMember(MetadataToken token, out IMetadataMember member)
        {
            return _cachedMembers.TryGetValue(token, out member);
        }

        internal void CacheMember(IMetadataMember member)
        {
            if (member.MetadataToken.Rid == 0)
                throw new ArgumentException("Cannot cache metadata members that do not have a metadata token yet.");
            _cachedMembers[member.MetadataToken] = member;
        }

        public IMetadataMember ResolveMember(MetadataToken token)
        {
            IMetadataMember member;
            if (!TryResolveMember(token, out member))
                throw new MemberResolutionException(string.Format("Invalid metadata token {0}.", token));
            return member;
        }

        public bool TryResolveMember(MetadataToken token, out IMetadataMember member)
        {
            if (!TryGetCachedMember(token, out member))
            {
                var tableStream = Header.GetStream<TableStream>();

                MetadataRow row;
                if (!tableStream.TryResolveRow(token, out row))
                    return false;

                member = tableStream.GetTable(token.TokenType).GetMemberFromRow(this, row);
                CacheMember(member);
            }

            return true;
        }
    }
}
