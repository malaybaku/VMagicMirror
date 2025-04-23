using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// UnityからWPFに送信するCommand,Queryの一覧
    /// </summary>
    public enum VmmServerCommands : ushort
    {
        Unknown = 0,
        
        SetUnityProcessId,
        ModelNameConfirmedOnLoad,
        RequestShowError,
        CloseConfigWindow,
        SetCalibrationFaceData,
        SetCalibrationFaceDataHighPower,
        MicrophoneVolumeLevel,
        AutoAdjustResults,
        UpdateDeviceLayout,
        ExtraBlendShapeClipNames,
        MidiNoteOn,
        ExTrackerCalibrateComplete,
        ExTrackerSetPerfectSyncMissedClipNames,
        ExTrackerSetIFacialMocapTroubleMessage,
        SetHandTrackingResult,
        //普通falseになるようにするため、ちょっと変な言い回しにしてます
        SetModelDoesNotSupportPen,
        UpdateAccessoryLayouts,
        UpdateCustomHandDownPose,
        NotifyVmcpReceiveStatus,
        EyeBlendShapeValues,

        // VRoid
        VRoidModelLoadCompleted,
        VRoidModelLoadCanceled, 

        // == LastCommandIdは常に最後尾に定義しておく ==
        LastCommandId,
    }
}
