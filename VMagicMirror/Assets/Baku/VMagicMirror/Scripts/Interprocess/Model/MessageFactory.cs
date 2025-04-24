using UnityEngine;

namespace Baku.VMagicMirror
{
    static class MessageFactory
    {
        public static Message SetUnityProcessId(int id) => Message.Int(VmmServerCommands.SetUnityProcessId, id);

        public static Message ModelNameConfirmedOnLoad(string modelName) => Message.String(VmmServerCommands.ModelNameConfirmedOnLoad, modelName);
        
        public static Message RequestShowError(string data) => Message.String(VmmServerCommands.RequestShowError, data);
        
        public static Message CloseConfigWindow() => Message.None(VmmServerCommands.CloseConfigWindow);

        public static Message SetCalibrationFaceData(string data) => Message.String(VmmServerCommands.SetCalibrationFaceData, data);
        public static Message SetCalibrationFaceDataHighPower(string data) => Message.String(VmmServerCommands.SetCalibrationFaceDataHighPower, data);

        public static Message MicrophoneVolumeLevel(int level) => Message.Int(VmmServerCommands.MicrophoneVolumeLevel, level);
        
        public static Message AutoAdjustResults(AutoAdjustParameters parameters) => Message.String(VmmServerCommands.AutoAdjustResults, JsonUtility.ToJson(parameters));
        
        public static Message UpdateDeviceLayout(DeviceLayoutsData data) => Message.String(VmmServerCommands.UpdateDeviceLayout, JsonUtility.ToJson(data));

        public static Message ExtraBlendShapeClipNames(string names) => Message.String(VmmServerCommands.ExtraBlendShapeClipNames, names);

        public static Message MidiNoteOn(int noteNumber) => Message.Int(VmmServerCommands.MidiNoteOn, noteNumber);

        public static Message ExTrackerCalibrateComplete(string data) => Message.String(VmmServerCommands.ExTrackerCalibrateComplete, data);
        
        public static Message ExTrackerSetPerfectSyncMissedClipNames(string data) => Message.String(VmmServerCommands.ExTrackerSetPerfectSyncMissedClipNames, data);

        public static Message ExTrackerSetIFacialMocapTroubleMessage(string message) => Message.String(VmmServerCommands.ExTrackerSetIFacialMocapTroubleMessage, message);

        public static Message SetHandTrackingResult(string json) => Message.String(VmmServerCommands.SetHandTrackingResult, json);

        //普通falseになるようにするため、ちょっと変な言い回しにしてます
        public static Message SetModelDoesNotSupportPen(bool doesNotSupport) => Message.Bool(VmmServerCommands.SetModelDoesNotSupportPen, doesNotSupport);
        
        public static Message UpdateAccessoryLayouts(string json) => Message.String(VmmServerCommands.UpdateAccessoryLayouts, json);

        public static Message UpdateCustomHandDownPose(string json) => Message.String(VmmServerCommands.UpdateCustomHandDownPose, json);
        
        public static Message NotifyVmcpReceiveStatus(string json) => Message.String(VmmServerCommands.NotifyVmcpReceiveStatus, json);

        public static Message EyeBlendShapeValues(string json) => Message.String(VmmServerCommands.EyeBlendShapeValues, json);

        public static Message NotifyBuddy2DLayout(string json) => Message.String(VmmServerCommands.NotifyBuddy2DLayout, json);
        public static Message NotifyBuddy3DLayout(string json) => Message.String(VmmServerCommands.NotifyBuddy3DLayout, json);

        public static Message NotifyBuddyLog(string json) => Message.String(VmmServerCommands.NotifyBuddyLog, json);
        
        #region VRoid

        public static Message VRoidModelLoadCompleted(string modelInfo) => Message.String(VmmServerCommands.VRoidModelLoadCompleted, modelInfo);
        public static Message VRoidModelLoadCanceled() => Message.None(VmmServerCommands.VRoidModelLoadCanceled);

        #endregion

    }
}