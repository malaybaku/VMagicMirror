using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    //NOTE: DIライブラリでもいいけどコレくらいなら…ということで自前クラスで賄ってるやつ
    //指定した型をインスタンスベースで登録するだけの簡素な仕組みを用いる。
    //コレで用が足りなくなったら普通のDIライブラリに差し替える        

    /// <summary>
    /// インスタンスベースで型1つにつき最大1つの要素を依存対象として登録できるやつだよ
    /// </summary>
    class ModelResolver
    {
        //あまり高頻度で呼ばない想定なのでいい加減でOK
        record BoundInstance(Type Type, object Instance);

        static ModelResolver? _instance;
        public static ModelResolver Instance => _instance ??= new ModelResolver();
        private ModelResolver() { }

        private readonly DependencyObject _designModeChecker = new DependencyObject();
        private readonly HashSet<BoundInstance> _registeredInstances = new HashSet<BoundInstance>();

        public void Add<T>(T model)
        {
            if (model == null)
            {
                throw new ArgumentNullException($"null instance was bound, {typeof(T)}");
            }
            _registeredInstances.Add(new BoundInstance(typeof(T), model));
        }

        public T Resolve<T>()
        {
            if (DesignerProperties.GetIsInDesignMode(_designModeChecker))
            {
                //NOTE: 実行時には起きないため、nullでも良いものとする
                return default!;
            }

            var result = _registeredInstances.FirstOrDefault(i => i.Type == typeof(T));
            if (result == null)
            {
                throw new InvalidOperationException($"Specified type is not bound, {typeof(T)}");
            }
            return (T)result.Instance;
        }        
    }
}
