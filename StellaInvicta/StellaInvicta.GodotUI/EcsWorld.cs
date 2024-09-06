using Flecs.NET.Core;
using Godot;
using StellaInvicta.GameLogic.Functions;
using StellaInvicta.Module;
using System;


namespace StellaInvicta.GodotUI
{
    
    public partial class EcsWorld : Node
    {
        [Export(PropertyHint.Range, "1,31,1")] // Range from 1 to 31, with a step of 1
        public int StartDay = 1;

        [Export(PropertyHint.Range, "1,12,1")] // Range from 1 to 12, with a step of 1
        public int StartMonth = 1;

        [Export(PropertyHint.Range, "0,9999,1")] // Range from 0 to 9999, with a step of 1 (adjust as needed)
        public int StartYear = 1;

        private World _world;
        public World World
        {
            get => _world;
        }


        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _world = World.Create("Stella Invicta");
            _world.SetThreads(System.Environment.ProcessorCount);
            _world.Import<Game>();
            _world.Set<Date>(new(day: StartDay, month: StartMonth, year: StartYear));

            Entity carlovingians = _world.Entity("Carlovingians")
                .Set<Created, Date>(new Date(day: 21, month: 2, year: 542))
                .Set<Name>(new("Carlovingians"))
                .Set<Age>(new(250));

            Entity charlemagne = _world.Entity("Charlemagne")
                                    .Add<Culture>(Cultures.Get(_world, nameof(BibliothecaVivens)))
                                    .Add<Ideology>(Ideologies.Get(_world, nameof(PaxUniversalis)))
                                    .Add<Species>(Specien.Get(_world, nameof(HomoSapiens)))
                                    .Add<Education>(Educations.Get(_world, nameof(BrilliantStrategist)))
                                    .Add<Trait>(Traits.Get(_world, nameof(Kind)))
                                    .Add<Trait>(Traits.Get(_world, nameof(Decadent)))
                                    .Add<Trait>(Traits.Get(_world, nameof(Robust)))
                                    .Add<Alive>()
                                    .Add<House>(carlovingians)
                                    .Set<Health>(new())
                                    .Set<PersonalCombatSkill>(new())
                                    .Set<Diplomacy>(new())
                                    .Set<Martial>(new())
                                    .Set<Stewardship>(new())
                                    .Set<Intrigue>(new())
                                    .Set<Learning>(new())
                                    .Set<Fertility>(new())
                                    .Set<Rationality>(new())
                                    .Set<Zeal>(new())
                                    .Set<Piety>(new(0))
                                    .Set<Wealth>(new(0))
                                    .Set<Prestige>(new(0))
                                    .Set<Age>(new(54))
                                    .Set<Birthday, Date>(new Date(day: 2, month: 4, year: 748));
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            _world.Progress((float) delta);
        }
    }
}
