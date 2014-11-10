using System.Linq;

namespace EngineTimeControl
{
    public class EngineControl : PartModule
    {
        private ModuleEngines _module;

        private bool _flag;    

        [UI_FloatRange(controlEnabled = true, maxValue = 100, minValue = 1, stepIncrement = 1, scene = UI_Scene.Editor)]
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Fuel percentage", isPersistant = true, guiUnits = "%")]
        public float PercentageExecute;

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            
            if (_flag || _module == null || !_module.EngineIgnited || PercentageExecute == 0) return;

            var t =
                _module.propellants.Select(
                    x => new { name = x.name, amount = x.totalResourceAvailable / x.totalResourceCapacity }).ToList();

            if (!t.Any(x => x.amount*100 < PercentageExecute)) return;

            _module.allowRestart = false;
            _module.Shutdown();
            _module.isEnabled = false;

            _flag = true;
        }

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
        }

    }
}
