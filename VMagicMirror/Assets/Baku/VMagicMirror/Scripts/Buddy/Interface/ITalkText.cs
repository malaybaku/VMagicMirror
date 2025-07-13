using System;
using System.Collections.Generic;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="ITalkTextItem"/> として表現される、会話テキストの要素の種類を表す列挙型です。
    /// </summary>
    public enum TalkTextItemType
    {
        /// <summary> テキストを表示するような要素です。 </summary>
        Text,
        /// <summary> テキストの状態を変化させず、待機するような要素です。 </summary>
        Wait,
    }
    
    /// <summary>
    /// <see cref="ITalkText.ShowText"/> 等で表現される、会話テキストの要素を表すインターフェースです。
    /// </summary>
    /// <remarks>
    /// この要素では、会話のテキストそのものや、会話の合間に挟まれる空白の待ち時間を個別の要素として表現します。
    /// </remarks>
    public interface ITalkTextItem
    {
        /// <summary>
        /// この要素の種類を取得します。
        /// </summary>
        TalkTextItemType Type { get; }
        
        /// <summary>
        /// <see cref="ITalkText.ShowText"/> で <c>key</c> パラメータに指定した値を取得します。
        /// </summary>
        string Key { get; }

        /// <summary>
        /// この要素に対応するテキストを取得します。
        /// </summary>
        /// <remarks>
        /// 要素が会話ではなく、定数時間の待機などを表現する要素の場合、この値は空文字列になります。
        /// </remarks>
        string Text { get; }

        /// <summary> この要素について、ユーザー入力がない場合に完了までに要する時間を秒単位で取得します。 </summary>
        /// <remarks>
        /// この値は一括で表示したテキスト等に対しては 0 になります。
        /// </remarks>
        float ScheduledDuration { get; }
    }
    
    /// <summary>
    /// <see cref="ISprite2D"/> 等で表現されたサブキャラの会話テキストを表示するAPIです。
    /// </summary>
    /// <remarks>
    /// このAPIでは <see cref="ShowText"/> 等の関数を通じて、会話のキューを処理できます。
    /// 一連の会話は、一括で <see cref="ShowText"/> や
    /// </remarks>
    public interface ITalkText
    {
        /// <summary>
        /// <see cref="ShowText"/> 等で指定したテキスト要素の表示を開始したときに発火します。
        /// </summary>
        event Action<ITalkTextItem> ItemDequeued;
        
        /// <summary>
        /// <see cref="ShowText"/> 等で指定したテキスト要素の表示を完了したときに発火します。
        /// </summary>
        event Action<ITalkTextItem> ItemFinished;

        /// <summary>
        /// 現在表示または処理しているテキスト要素を取得します。処理中の要素がない場合、 <c>null</c> を返します。
        /// </summary>
        /// <returns></returns>
        ITalkTextItem GetCurrentPlayingItem();
        
        /// <summary>
        /// 表示または処理待ちしている要素の数を取得します。
        /// この結果には <see cref="GetCurrentPlayingItem" /> で取得できる要素の数も含まれます。
        /// </summary>
        int QueueCount { get; }
        
        /// <summary>
        /// 指定したテキストを表示します。
        /// </summary>
        /// <param name="text">表示するテキストです。</param>
        /// <param name="speed">テキストを文字送りするときの、1秒あたりの文字数を指定します。ただし、値の符号に応じて挙動が変化します。既定値は <c>10.0f</c> です。</param>
        /// <param name="waitAfterCompleted">テキストの表示後に次のテキスト等を表示するまでの時間を秒単位で指定します。既定値は <c>8.0f</c> です。</param>
        /// <param name="key">
        /// <see cref="ItemDequeued"/> や <see cref="ItemFinished"/> で渡される <see cref="ITalkTextItem.Key"/> の値となるキー文字列です。
        /// <c>""</c> を指定した場合、連番ベースで一意識別子を生成します。既定値は <c>""</c> です。
        /// </param>
        /// <returns>
        /// <paramref name="key"/> で空ではない場合、その値を返します。
        /// <paramref name="key"/> が空だった場合、自動で生成した一意識別子を返します。
        /// </returns>
        /// <remarks>
        /// <para>
        /// この関数の呼び出しによって内部的なキューにテキスト要素が追加され、サブキャラのセリフとして表示されるようになります。
        /// 指定したテキストを直ちに表示したい場合は <see cref="Clear"/> を併用します。
        /// </para>
        /// 
        /// <para>
        /// <paramref name="speed"/> では文字送りのスピードを指定できます。
        /// 正の値を指定した場合、位置秒一定のスピードで文字送りを行います。
        /// 0以下の値を指定した場合、文字送りは行わず、テキストを一括で表示します。
        /// </para>
        /// </remarks>
        string ShowText(string text, float speed = 10f, float waitAfterCompleted = 8f, string key = "");

        /// <summary>
        /// テキストの表示状態を変化させずに待機するような処理を指示します。
        /// </summary>
        /// <param name="duration">待ち時間</param>
        /// <param name="key">
        /// <see cref="ItemDequeued"/> や <see cref="ItemFinished"/> で渡される <see cref="ITalkTextItem.Key"/> の値となるキー文字列です。
        /// <c>""</c> を指定した場合、連番ベースで一意識別子を生成します。既定値は <c>""</c> です。
        /// </param>
        /// <returns>
        /// <paramref name="key"/> で空ではない場合、その値を返します。
        /// <paramref name="key"/> が空だった場合、自動で生成した一意識別子を返します。
        /// </returns>
        /// <remarks>
        /// このメソッドは、<see cref="ShowText"/> よりも詳細に待ち時間を管理する際に使用できます。
        /// </remarks>
        string Wait(float duration, string key = "");
        
        /// <summary>
        /// 表示待ち中のテキスト要素を削除します。
        /// </summary>
        /// <param name="includeCurrentItem">
        /// <c>true</c> を指定すると、現在表示中のアイテムも削除し、ただちにデフォルト状態になります。既定値は <c>true</c> です。
        /// </param>
        /// <remarks>
        /// 会話を中断して新規の会話を始めたい場合、この関数を用いて既存のテキスト要素をリセットできます。
        /// </remarks>
        void Clear(bool includeCurrentItem = true);
    }
}
