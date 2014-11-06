using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fasterflect;
using UnityEngine;

namespace EngineTimeControl
{
    public class EngineControl : PartModule
    {
        private ModuleEngines _module;

        private StartState _state;


        [UI_FloatRange(controlEnabled = true, maxValue = 100, minValue = 0, stepIncrement = 1)]
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Fuel percentage", isPersistant = true)]
        public float TimeExecute;

        public override void OnAwake()
        {
            //GameEvents.onPartAttach.Add(PartAttached);
        }

        //private void PartAttached(GameEvents.HostTargetAction<Part, Part> data)
        //{
        //    if (data.host == part)
        //    {
        //        ScreenMessages.PostScreenMessage("Part attached");
        //        CalculateFuel();
        //    }
        //}

        public override void OnStart(StartState state)
        {
            _state = state;

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

        private void CalculateFuel()
        {
            if ((_state & StartState.Editor) != StartState.Editor) return;
            
            var parts = EditorLogic.fetch.ship.Parts.Where(x => x.inverseStage == part.inverseStage);

            var res = parts.SelectMany(x => x.Resources.list).GroupBy(x => x.resourceName).Select(x => new { key = x.Key, value = x.Sum(y => y.amount) }).ToList();

            int result = 0;
            foreach (var propellant in _module.propellants)
            {
                var buff = res.FirstOrDefault(x => x.key.Contains(propellant.name)).value / propellant.ratio;
                if (buff > result) result = (int) buff;
            }

            if (Fields["TimeExecute"].uiControlEditor != null)
            {
                var control = Fields["TimeExecute"].uiControlEditor as UI_FloatRange;
                if (control != null)
                {
                    control.maxValue = result;
                }
            }
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

            //var engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault();

            //if (engine == null) yield break;

            _module.allowRestart = false;
            _module.Shutdown();
            _module.isEnabled = false;
        }


    }
}
