using System;
using System.Collections;
using System.Linq;
using Fasterflect;
using UnityEngine;

namespace EngineTimeControl
{
    public class EngineControl : PartModule
    {
        private ModuleEngines _module;

        [UI_FloatRange(controlEnabled = true, maxValue = 10, minValue = 1, stepIncrement = 1)]
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Time executed", isPersistant = true)]
        public float TimeExecute;

        public override void OnStart(StartState state)
        {
            if (!part.Modules.OfType<ModuleEngines>().Any()) return;

            _module = part.Modules.OfType<ModuleEngines>().First();

            if (part.Modules.IndexOf(_module) > part.Modules.IndexOf(this))
            {
                part.Modules.Remove(this);
                part.Modules.Add(this);
            }
            
            base.OnStart(state);

            AttachToEvent();
        }

        public override void OnActive()
        {
            base.OnActive();

            if (_module.EngineIgnited)
            {
                EngineActivated();
            }
        }


        private void AttachToEvent()
        {
            var s = _module.Events["Activate"];

            _module.Events.Remove(s);

            var method = s.GetPropertyValue("onEvent", Flags.InstanceAnyVisibility);

            if (method == null)
            {
                Debug.LogError("onEvent is null");
                return;
            }


            var info = method as BaseEventDelegate;

            if (info == null)
            {
                Debug.LogError("info is null");
                return;
            }

            var target = Delegate.Combine(info,
                Delegate.CreateDelegate(typeof(BaseEventDelegate), this, "EngineActivated", true));

            var bEvent = new BaseEvent(_module.Events, s.name, (BaseEventDelegate)target, new KSPEvent()
            {
                active = s.active,
                category = s.category,
                externalToEVAOnly = s.externalToEVAOnly,
                guiActive = s.guiActive,
                guiActiveEditor = s.guiActiveEditor,
                guiActiveUnfocused = s.guiActiveUnfocused,
                guiIcon = s.guiIcon,
                guiName = s.guiName,
                name = s.name,
                unfocusedRange = s.unfocusedRange
            });

            _module.Events.Add(bEvent);
        }

        public void EngineActivated()
        {
            StartCoroutine(CheckTime());
        }

        private IEnumerator CheckTime()
        {
            while (TimeExecute > 0)
            {
                TimeExecute--;

                yield return new WaitForSeconds(1f);
            }

            var engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault();

            if (engine == null) yield break;

            engine.allowRestart = false;
            engine.Shutdown();
            engine.isEnabled = false;
        }
    }
}
