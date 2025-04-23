using UnityEngine;

namespace Baku.VMagicMirror
{
    class MessageFactory
    {
        //Singleton
        private static MessageFactory _instance;
        public static MessageFactory Instance => _instance ??= new MessageFactory();
        private MessageFactory() { }

        public Message SetUnityProcessId(int id) => Message.String(VmmServerCommands.SetUnityProcessId, id.ToString());

        public Message ModelNameConfirmedOnLoad(string modelName) => Message.String(VmmServerCommands.ModelNameConfirmedOnLoad, modelName);
        
        public Message RequestShowError(string data) => Message.String(VmmServerCommands.RequestShowError, data);
        
        public Message CloseConfigWindow() => Message.None(VmmServerCommands.CloseConfigWindow);

        public Message SetCalibrationFaceData(string data) => Message.String(VmmServerCommands.SetCalibrationFaceData, data);
        public Message SetCalibrationFaceDataHighPower(string data) => Message.String(VmmServerCommands.SetCalibrationFaceDataHighPower, data);

        public Message MicrophoneVolumeLevel(int level) => Message.String(VmmServerCommands.MicrophoneVolumeLevel, $"{level}");
        
        public Message AutoAdjustResults(AutoAdjustParameters parameters) => Message.String(VmmServerCommands.AutoAdjustResults, JsonUtility.ToJson(parameters));
        
        public Message UpdateDeviceLayout(DeviceLayoutsData data) => Message.String(VmmServerCommands.UpdateDeviceLayout, JsonUtility.ToJson(data));

        public Message ExtraBlendShapeClipNames(string names) => Message.String(VmmServerCommands.ExtraBlendShapeClipNames, names);

        public Message MidiNoteOn(int noteNumber) => Message.String(VmmServerCommands.MidiNoteOn, $"{noteNumber}");

        public Message ExTrackerCalibrateComplete(string data) => Message.String(VmmServerCommands.ExTrackerCalibrateComplete, data);
        
        public Message ExTrackerSetPerfectSyncMissedClipNames(string data) => Message.String(VmmServerCommands.ExTrackerSetPerfectSyncMissedClipNames, data);

        public Message ExTrackerSetIFacialMocapTroubleMessage(string message) => Message.String(VmmServerCommands.ExTrackerSetIFacialMocapTroubleMessage, message);

        public Message SetHandTrackingResult(string json) => Message.String(VmmServerCommands.SetHandTrackingResult, json);

        //普通falseになるようにするため、ちょっと変な言い回しにしてます
        public Message SetModelDoesNotSupportPen(bool doesNotSupport) => Message.String(VmmServerCommands.SetModelDoesNotSupportPen, $"{doesNotSupport}");
        
        public Message UpdateAccessoryLayouts(string json) => Message.String(VmmServerCommands.UpdateAccessoryLayouts, json);

        public Message UpdateCustomHandDownPose(string json) => Message.String(VmmServerCommands.UpdateCustomHandDownPose, json);
        
        public Message NotifyVmcpReceiveStatus(string json) => Message.String(VmmServerCommands.NotifyVmcpReceiveStatus, json);

        public Message EyeBlendShapeValues(string json) => Message.String(VmmServerCommands.EyeBlendShapeValues, json);
        
        #region VRoid

        public Message VRoidModelLoadCompleted(string modelInfo) => Message.String(VmmServerCommands.VRoidModelLoadCompleted, modelInfo);
        public Message VRoidModelLoadCanceled() => Message.None(VmmServerCommands.VRoidModelLoadCanceled);

        #endregion

    }
}