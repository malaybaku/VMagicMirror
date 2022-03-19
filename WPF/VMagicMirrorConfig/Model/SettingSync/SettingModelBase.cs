using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// Entityのデータ読み書きを、Unity側とのデータ同期もしながら実行するクラス。
    /// Entityのデータだけでなく、設定ファイルに保存しないような一時的なフラグも扱える。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    abstract class SettingModelBase<TEntity> : NotifiableBase
        where TEntity : SettingEntityBase, new()
    {
        public SettingModelBase(IMessageSender sender)
        {
            _sender = sender;
        }

        private readonly IMessageSender _sender;
        /// <summary>
        /// Unityに何かしらのメッセージを送信します。
        /// </summary>
        /// <param name="msg"></param>
        protected void SendMessage(Message msg) => _sender.SendMessage(msg);

        /// <summary>
        /// Unityにクエリを送り、戻り値を取得します。
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected Task<string> SendQueryAsync(Message msg) => _sender.QueryMessageAsync(msg);

        /// <summary>Load()が完全に完了すると発火します。</summary>
        public event EventHandler? Loaded;

        /// <summary>設定を初期状態に戻します。</summary>
        public abstract void ResetToDefault();

        /// <summary>
        /// Load()を呼び出してから呼び出し完了するまでのあいだtrueになるフラグ。
        /// ファイルロード中にViewModelが一部データのコピーを行えないタイミングがあるので、
        /// それを避けたい場合に参照する
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>セーブ前に行いたい処理(主にJSONシリアライズ等)があればここで実装します。</summary>
        protected virtual void PreSave() { }

        /// <summary>セーブ後、かつLoadedイベントより前に行う処理。値の代入漏れのカバーやデータのデシリアライズが可能です</summary>
        protected virtual void AfterSave(TEntity entity) { }

        /// <summary>ロード直前に行いたい処理があれば実装します。</summary>
        protected virtual void PreLoad() { }

        /// <summary>ロード直後に行いたい処理(デシリアライズや処理漏れの対処)があれば実装します。</summary>
        /// <param name="entity"></param>
        protected virtual void AfterLoad(TEntity entity) { }

        /// <summary>
        /// ファイル等から読み込んだデータをロード、適用します。
        /// </summary>
        /// <param name="entity"></param>
        public void Load(TEntity? entity)
        {
            if (entity == null)
            {
                LogOutput.Instance.Write($"Load Requested for {typeof(TEntity).Name}, but entity is invalid");
                return;
            }

            IsLoading = true;
            SearchPropertyRoutine((source, target) =>
            {
                if (!(target.PropertyType.IsGenericType && target.PropertyType.GetGenericTypeDefinition() == typeof(RProperty<>)))
                {
                    //モデル側が組み込み型で値を持っているケース: 単に代入でOK
                    target.SetValue(this, source.GetValue(entity));
                    return;
                }

                //TargetがRPropertyMin<T>であると考えられるケース: Valueに向かって値を入れる
                var rProperty = target.GetValue(this);
                //NOTE: nameofの中のintに意味はない(型名を入れないとコンパイラが怒るから入れてるだけです)
                var valueProperty = rProperty?.GetType()?.GetProperty(nameof(RProperty<int>.Value));

                if (rProperty == null || valueProperty == null)
                {
                    LogOutput.Instance.Write(
                        $"WARN: Property '{source.Name}' is defined in model, and not RPropertyMin<> but some generics type"
                        );
                    return;
                }

                valueProperty.SetValue(rProperty, source.GetValue(entity));
            });

            AfterLoad(entity);
            IsLoading = false;
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// モデルからエンティティ情報を生成し、ファイルセーブできるデータに変換します。
        /// </summary>
        /// <returns></returns>
        public TEntity Save()
        {
            PreSave();
            var entity = new TEntity();

            SearchPropertyRoutine((entityProp, modelProp) =>
            {
                //TargetがRPropertyMin<T>ならValueに向かって値を入れる。
                //そうでなければ直接Setterを呼ぶ
                if (!(modelProp.PropertyType.IsGenericType && modelProp.PropertyType.GetGenericTypeDefinition() == typeof(RProperty<>)))
                {
                    //モデル側も組み込み型で値を持っている: 単に代入
                    entityProp.SetValue(entity, modelProp.GetValue(this));
                    return;
                }

                //TargetがRPropertyMin<T>であると考えられるケース: Valueから値をとってEntityに代入
                var rProperty = modelProp.GetValue(this);
                //NOTE: nameofの中のintに意味はない(型名を入れないとコンパイラが怒るから入れてるだけです)
                var valueProperty = rProperty?.GetType()?.GetProperty(nameof(RProperty<int>.Value));

                if (rProperty == null || valueProperty == null)
                {
                    //NOTE: ここを通過した場合、モデル層のプロパティが足りてないはず
                    LogOutput.Instance.Write(
                        $"WARN: Property '{entityProp.Name}' is defined in model, and not RPropertyMin<> but some generics type"
                        );
                    return;
                }

                //Entityの値(組み込み型)をRPropertyMinのValueに代入
                entityProp.SetValue(entity, valueProperty.GetValue(rProperty));
            });

            AfterSave(entity);
            return entity;
        }

        //Entityクラスのプロパティ名を走査し、その名前のプロパティがモデルにもあったら指定された関数を呼ぶルーチンです。
        //ロードとセーブで走査の枠組みは共通なため、そこを切り出してます
        private void SearchPropertyRoutine(Action<PropertyInfo, PropertyInfo> onPropertyMatch)
        {
            //NOTE: Entityはgetter/setterしか持ってないのでかなりザツに走査しても大丈夫
            foreach (var entityProp in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (typeof(SettingEntityBase).IsAssignableFrom(entityProp.PropertyType))
                {
                    //NOTE: ここを通るのはLayoutにぶら下がったGamepadの設定のケース。
                    //今のところ珍しいケースなためスルーし、代わりにAfterLoad処理とかでどうにかしてもらう
                    continue;
                }

                try
                {
                    //NOTE: Model側は同名のプロパティ(RPropertyMin<T>か生の型)で値を受け取れるようになってるはずなので探す
                    var modelProp = GetType().GetProperty(entityProp.Name);
                    if (modelProp == null)
                    {
                        LogOutput.Instance.Write($"WARN: Property '{entityProp.Name}' is defined in entity but not in model");
                        continue;
                    }

                    onPropertyMatch(entityProp, modelProp);
                }
                catch (Exception exOnPropCopy)
                {
                    //NOTE: 一応プロパティ個別にガードしてはいるが、ここに到達する頻度が高いならコードを直すべき
                    LogOutput.Instance.Write(exOnPropCopy);
                }
            }
        }
    }
}
