﻿using FSO.Common.Content;
using FSO.Content.Framework;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    /// <summary>
    /// A gateway into BCF resources, accessible in a similar manner to the TSO resources.
    /// Must first scan all content to find the "files" each animation or skin is present in.
    /// </summary>
    public class TS1BCFProvider
    {
        public TS1SubProvider<BCF> BCFProvider;
        public TS1SubProvider<CFP> CFPProvider;
        public TS1Provider BaseProvider;
        public Dictionary<string, string> AnimHostBCF = new Dictionary<string, string>();
        public Dictionary<string, string> SkinHostBCF = new Dictionary<string, string>();
        public Dictionary<string, string> SkelHostBCF = new Dictionary<string, string>();
        public Content ContentManager;

        public TS1BCFProvider(Content contentManager, TS1Provider provider)
        {
            ContentManager = contentManager;
            BaseProvider = provider;
            BCFProvider = new TS1SubProvider<BCF>(provider, ".bcf");
            CFPProvider = new TS1SubProvider<CFP>(provider, ".cfp");
        }

        public void Init()
        {
            BCFProvider.Init();
            CFPProvider.Init();

            //scan system for named items

            var allBCFs = BCFProvider.ListGeneric();
            foreach (var bcf in allBCFs)
            {
                var file = (BCF)bcf.GetThrowawayGeneric();
                foreach (var anim in file.Animations)
                {
                    AnimHostBCF[anim.Name.ToLowerInvariant()] = Path.GetFileName(bcf.ToString().ToLowerInvariant().Replace('\\', '/'));
                }
                foreach (var skin in file.Appearances)
                {
                    SkinHostBCF[skin.Name.ToLowerInvariant()] = Path.GetFileName(bcf.ToString().ToLowerInvariant().Replace('\\', '/'));
                }
                foreach (var skel in file.Skeletons)
                {
                    SkelHostBCF.Add(skel.Name.ToLowerInvariant(), Path.GetFileName(bcf.ToString().ToLowerInvariant().Replace('\\', '/')));
                }
            }
        }

        public object Get(string name, Type expected)
        {
            if (name == null) return null;
            if (expected == typeof(Animation))
            {
                name = name.Substring(0, name.Length - 5).ToLowerInvariant(); //remove .anim
                string filename = null;
                if (AnimHostBCF.TryGetValue(name, out filename))
                {
                    var bcf = BCFProvider.Get(filename);
                    var anim = bcf.Animations.FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
                    if (anim == null) return null;
                    if (anim.Translations == null)
                    {
                        //enrich animation with CFP
                        var cfp = CFPProvider.Get((anim.XSkillName + ".cfp").ToLowerInvariant());
                        if (cfp == null) return null;
                        cfp.EnrichAnim(anim);
                    }
                    return anim;
                }
                return null;
            }
            else if (expected == typeof(Appearance))
            {
                name = name.Substring(0, name.Length - 4).ToLowerInvariant(); //remove .apr
                string filename = null;
                if (SkinHostBCF.TryGetValue(name, out filename))
                {
                    var bcf = BCFProvider.Get(filename);
                    var skin = bcf.Appearances.FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
                    return skin;
                }
                return null;
            }
            else if (expected == typeof(Skeleton))
            {
                name = name.Substring(0, name.Length - 5).ToLowerInvariant(); //remove .skel
                string filename = null;
                if (SkelHostBCF.TryGetValue(name, out filename))
                {
                    var bcf = BCFProvider.Get(filename);
                    var skel = bcf.Skeletons.FirstOrDefault(x => x.Name.ToLowerInvariant() == name);
                    return skel;
                }
                return null;
            }
            throw new NotImplementedException();
        }
    }

    public class TS1BCFSubProvider<T> : IContentProvider<T>
    {
        TS1BCFProvider BaseProvider;
        public TS1BCFSubProvider(TS1BCFProvider baseProvider)
        {
            BaseProvider = baseProvider;
        }

        public T Get(string name)
        {
            return (T)BaseProvider.Get(name, typeof(T));
        }

        public T Get(ulong id, bool ts1)
        {
            throw new NotImplementedException();
        }

        public T Get(uint type, uint fileID, bool ts1)
        {
            throw new NotImplementedException();
        }

        public List<IContentReference<T>> List()
        {
            return new List<IContentReference<T>>();
        }

        public T Get(ContentID id)
        {
            if (id.FileName != null) return Get(id.FileName);
            return default(T);
        }
    }

    public class TS1BCFAnimationProvider : TS1BCFSubProvider<Animation>
    {
        public TS1BCFAnimationProvider(TS1BCFProvider baseProvider) : base(baseProvider)
        {
        }
    }

    public class TS1BCFSkeletonProvider : TS1BCFSubProvider<Skeleton>
    {
        public TS1BCFSkeletonProvider(TS1BCFProvider baseProvider) : base(baseProvider)
        {
        }
    }

    public class TS1BCFAppearanceProvider : TS1BCFSubProvider<Appearance>
    {
        public TS1BCFAppearanceProvider(TS1BCFProvider baseProvider) : base(baseProvider)
        {
        }
    }
}
