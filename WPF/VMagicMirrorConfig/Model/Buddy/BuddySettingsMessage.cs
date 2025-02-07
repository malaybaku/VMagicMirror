using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig.BuddySettingsMessages
{
    //NOTE:
    // - MessageFactory�o�R��Unity�ɓ�����f�[�^���������Ē�`���Ă�
    // - �g���N���X�����Ȃ�(BuddySettingsSender�݂̂̑z��)�̂ŁA���̃N���X���namespace����[��
    [Serializable]
    public class BuddySettingsMessage
    {
        public string BuddyId { get; set; } = "";
        public BuddySettingsPropertyMessage[] Properties { get; set; } = Array.Empty<BuddySettingsPropertyMessage>();
    }

    // NOTE: �G�Ƀf�[�^�ʂ����ړI�ŁAJSON�͂Ȃ�ׂ����L����B
    // �Ƃ��� ~~Value �͊���l�̂Ƃ��AType�ƍ��v����Value���ȗ�����B
    //
    // ��: BuddySettingsMessage.Property �̗v�f�Ƃ��āubool��false�v���w�肷��ꍇ�A
    // Name��Type(="Bool")������JSON�̃L�[�Ƃ��ē���̂��z�苓��
    [Serializable]
    public class BuddySettingsPropertyMessage
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? BuddyId { get; set; }

        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool BoolValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int IntValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float FloatValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? StringValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyVector2 Vector2Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyVector3 Vector3Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyTransform2D Transform2DValue { get; set; }

        //TODO?: ParentBone�̃V���A���C�Y��int�ɂȂ肻���Ȃ̂͂�����Ɛ����͈����B�ň����߂Ă�Unity���Ŏ�ɕ����邯��
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyTransform3D Transform3DValue { get; set; }
    }
}
