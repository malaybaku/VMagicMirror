using System.Collections.Generic;
using System.Threading.Tasks;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// サブキャラとしてVRMのロードと操作を行うAPIです。
    /// 本APIは作成途上のものであり、VMagicMirror v4.0.0の時点では本APIは利用できません。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本APIはVRMのロードと操作を行うためのAPIです。
    /// VMagicMirror v4.0.0の時点では機能整備が完了していないため、本APIの利用手段は提供していません。
    /// ここでは、想定している機能を提示する目的でドキュメントを公開しています。
    /// </para>
    ///
    /// <para>
    /// VRMやGLBによる3Dオブジェクトをサブキャラとして表示する機能は、 v4.0.0 以降のマイナーアップデートとして提供予定です。
    /// </para>
    /// </remarks>
    public interface IVrm
    {
        /// <summary> オブジェクトの基本姿勢に関する値を取得します。 </summary>
        ITransform3D Transform { get; }
        
        /// <summary>
        /// ファイルパスを指定してVRMをロードします。
        /// </summary>
        /// <param name="path">VRMのファイルパス</param>
        /// <returns></returns>
        Task LoadAsync(string path);

        /// <summary>
        /// 名称を指定してプリセットVRMをロードします。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// プリセットVRMとは、アプリケーション本体に組み込まれたVRMのことです。
        /// このメソッドでVRMをロードする場合、事前にVRMを用意する必要はありません。
        /// 
        /// v4.0.0では <paramref name="name"/> に指定できる値は <c>"A"</c> のみです。
        /// これ以外の名称を指定した場合、 <see cref="LoadAsync"/> で存在しないファイルを指定した場合と同様のエラーになります。
        /// </remarks>
        Task LoadPresetAsync(string name);
        
        /// <summary>
        /// <see cref="LoadAsync"/> でロードしたモデルを表示します。
        /// </summary>
        void Show();

        /// <summary>
        /// モデルを非表示にします。
        /// </summary>
        void Hide();
        
        // ポーズ
        /// <summary>
        /// ボーンのローカル回転を設定します。
        /// </summary>
        /// <param name="bone">回転を設定するボーン</param>
        /// <param name="localRotation">ボーンのローカル回転</param>
        /// <remarks>
        /// 指定したボーンが任意ボーンであり、そのボーンが存在しない場合、この関数を呼び出しても何も起こりません。
        /// </remarks>
        void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation);

        /// <summary>
        /// ロードしたモデルについて、Hipsボーンの位置をローカル座標扱いで指定してモデルの現在位置を更新します。
        /// </summary>
        /// <param name="position">Hipsボーンの位置</param>
        /// <remarks>
        /// <see cref="ITransform3D.SetParent(IReadOnlyTransform3D)"/> などで親オブジェクトを指定している場合、
        /// <paramref name="position"/> は親要素に対するローカルな位置として扱われます。
        /// そうでない場合、<paramref name="position"/> はワールド座標の位置として扱われます。
        /// </remarks>
        void SetHipsLocalPosition(Vector3 position);

        /// <summary>
        /// ロードしたモデルについて、Hipsボーンの位置をワールド座標扱いで指定してモデルの現在位置を更新します。
        /// </summary>
        /// <param name="position">Hipsボーンの位置</param>
        void SetHipsPosition(Vector3 position);

        // NOTE: このへんは「サブキャラが通信を受ける」とか「オレオレフォーマットのファイルを使って何か再生する」といったケースで使いうる
        
        /// <summary>
        /// ロードしたモデルについて、ボーンのローカル回転を一括で適用します。
        /// </summary>
        /// <param name="localRotations">回転を適用したいボーンをキー、ローカル回転を値としたディクショナリ</param>
        /// <remarks>
        /// ボーン回転を更新したくないボーンについては、キーを含めないようにします。
        /// 任意ボーンについて、モデルに該当するボーンが存在しない場合、その値は無視されます。
        /// </remarks>
        void SetBoneRotations(IReadOnlyDictionary<HumanBodyBones, Quaternion> localRotations);
        
        /// <summary>
        /// ロードしたモデルについて、モデルの姿勢をMuscle値ベースで適用します。
        /// </summary>
        /// <param name="muscles">Muscle値の一覧</param>
        /// <remarks>
        /// この関数はUnityのHumanoidの仕様を十分理解している場合に、効率的に姿勢を適用する目的で使用します。
        /// <paramref name="muscles"/> は長さが <c>95</c> の配列であるのが期待値です。
        /// muscleの一部のみを更新する場合、更新したいmuscleの値のみを有効な値とし、それ以外は <c>null</c> にします。
        /// </remarks>
        void SetMuscles(float?[] muscles);
        
        // 表情
        /// <summary>
        /// モデルに定義されたカスタムブレンドシェイプの名称の一覧を取得します。
        /// </summary>
        /// <returns></returns>
        string[] GetCustomBlendShapeNames();
        /// <summary>
        /// ロードしたモデルに指定した名称のカスタムブレンドシェイプが存在するかどうかを取得します。
        /// </summary>
        /// <param name="name">カスタムブレンドシェイプの名称</param>
        /// <returns>指定した名称のカスタムブレンドシェイプが存在すれば <c>true</c>、そうでなければ<c>false</c></returns>
        bool HasBlendShape(string name);

        //TODO: 表情とVRMAの双方で、補間について何か記述と実装が欲しい

        /// <summary>
        /// ロードしたモデルについて、ブレンドシェイプの現在値を取得します。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="customClip">VRM1.0の標準ブレンドシェイプの値を取得する場合は <c>false</c>、カスタムのブレンドシェイプの値を取得する場合は <c>true</c></param>
        /// <returns>現在適用されているブレンドシェイプの値</returns>
        /// <remarks>
        /// 指定したブレンドシェイプがモデルに存在しない場合、この関数は <c>0</c> を返します。
        /// </remarks>
        float GetBlendShape(string name, bool customClip);
        
        /// <summary>
        /// ロードしたモデルについて、ブレンドシェイプの値を適用します。
        /// </summary>
        /// <param name="name">ブレンドシェイプの名称</param>
        /// <param name="customClip">VRM1.0の標準ブレンドシェイプに値を適用する場合は <c>false</c>、カスタムのブレンドシェイプに適用する場合は <c>true</c></param>
        /// <param name="value">適用するブレンドシェイプの値。 [0, 1] の範囲で指定します</param>
        /// <remarks>
        /// 指定したブレンドシェイプがモデルに存在しない場合、この関数を呼んでも何も起こりません。
        /// </remarks>
        void SetBlendShape(string name, bool customClip, float value);

        // VRMA
        /// <summary>
        /// VRM Animationを再生します。
        /// </summary>
        /// <param name="animation">対象になるVRM Animation</param>
        /// <param name="loop">VRM Animationをループ実行したい場合は <c>true</c>、そうでなければ<c>false</c></param>
        /// <param name="immediate">補間を行わずにただちに指定したアニメーションを再生する場合は <c>true</c>、現在のポーズとの補間を行う場合は <c>false</c></param>
        /// <remarks>
        /// v4.0.0の時点では <paramref name="immediate"/> オプションの値によらず、補間を行わない動作が適用されます。
        /// このフラグは将来のバージョンで参照されるようになります。
        /// バージョンアップによる挙動変更を避けたい場合、 <paramref name="immediate"/> の値としては <c>true</c> を指定してください。
        /// </remarks>
        void RunVrma(IVrmAnimation animation, bool loop, bool immediate);
        
        /// <summary>
        /// <see cref="RunVrma"/> で再生したVRM Animationを停止します。
        /// </summary>
        /// <param name="immediate">補間を行わずにただちに停止させる場合は <c>true</c>、現在のポーズとの補間を行いながら停止させる場合は <c>false</c></param>
        /// <remarks>
        /// VRM Animationを再生していない状態でこの関数を呼び出した場合、何も起こりません。
        /// 
        /// v4.0.0の時点では <paramref name="immediate"/> オプションの値によらず、補間を行わない動作が適用されます。
        /// このフラグは将来のバージョンで参照されるようになります。
        /// バージョンアップによる挙動変更を避けたい場合、 <paramref name="immediate"/> の値としては <c>true</c> を指定してください。
        /// </remarks>
        void StopVrma(bool immediate);

    }
}
