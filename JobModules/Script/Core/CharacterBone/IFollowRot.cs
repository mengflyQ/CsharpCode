﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.Appearance;

namespace Core.CharacterBone
{
    public interface IFollowRot
    {
        void PreUpdate(FollowRotParam param, ICharacterBone characterBone);
        void SyncTo(ICharacterBoneState state);
    }
}
