using FluentAssertions;
using StateSmith.Output;
using StateSmith.Output.Algos.Balanced1;
using StateSmith.Output.UserConfig;
using StateSmith.Runner;
using StateSmith.SmGraph;
using System.IO;
using Xunit;

namespace StateSmithTest.Output.BalancedCoder1;

public class AlgoBalanced1Tests
{
    [Fact(Skip = "todo")]
    public void GilOutput()
    {
        string gil = BuildExampleGilFile(skipIndentation: false, out var sm);

        gil.ToString().ShouldBeShowDiff("""
            // StateMachine MySm1
            public class MySm1
            {
                public enum EventId
                {
                    EV1 = 0,
                    EV2 = 1,
                }
                
                public const int EventIdCount = 2;
                
                public enum StateId
                {
                    ROOT = 0,
                    S1 = 1,
                    S2 = 2,
                }
                
                public const int StateIdCount = 3;
                
                delegate void Func();
                
                // Used internally by state machine. Feel free to inspect, but don't modify.
                public StateId state_id;
                
                // Used internally by state machine. Don't modify.
                private Func ancestor_event_handler;
                
                // Used internally by state machine. Don't modify.
                private Func[] current_event_handlers = new Func[EventIdCount];
                
                // Used internally by state machine. Don't modify.
                private Func current_state_exit_handler;
                public MySm1()
                {
                }
                
                private void exit_up_to_state_handler(Func desired_state_exit_handler)
                {
                    while (current_state_exit_handler != desired_state_exit_handler)
                    {
                        current_state_exit_handler();
                    }
                }
                
                public void start()
                {
                    ROOT_enter();
                }
                
                public void dispatch_event(EventId event_id)
                {
                    Func behavior_func = current_event_handlers[(int)event_id];
                    
                    while (behavior_func != null)
                    {
                        ancestor_event_handler = null;
                        behavior_func();
                        behavior_func = ancestor_event_handler;
                    }
                }
                
                ////////////////////////////////////////////////////////////////////////////////
                // event handlers for state ROOT
                ////////////////////////////////////////////////////////////////////////////////
                
                private void ROOT_enter()
                {
                    // setup trigger/event handlers
                    current_state_exit_handler = ROOT_exit;
                }
                
                private void ROOT_exit()
                {
                    // State machine root is a special case. It cannot be exited.
                }
                
                
                ////////////////////////////////////////////////////////////////////////////////
                // event handlers for state S1
                ////////////////////////////////////////////////////////////////////////////////
                
                private void S1_enter()
                {
                    // setup trigger/event handlers
                    current_state_exit_handler = S1_exit;
                }
                
                private void S1_exit()
                {
                    // adjust function pointers for this state's exit
                    current_state_exit_handler = ROOT_exit;
                }
                
                
                ////////////////////////////////////////////////////////////////////////////////
                // event handlers for state S2
                ////////////////////////////////////////////////////////////////////////////////
                
                private void S2_enter()
                {
                    // setup trigger/event handlers
                    current_state_exit_handler = S2_exit;
                }
                
                private void S2_exit()
                {
                    // adjust function pointers for this state's exit
                    current_state_exit_handler = ROOT_exit;
                }
                
                
            }

            """);
    }

    public static string BuildExampleGilFile(bool skipIndentation, out StateMachine sm)
    {
        sm = new("TestsMySm1");
        var s1 = sm.AddChild(new State("S1"));
        var s2 = sm.AddChild(new State("S2"));

        sm.AddChild(new InitialState()).AddTransitionTo(s1).actionCode = "this.vars.b = false;";
        s1.AddTransitionTo(s2);

        sm.variables += "bool b;";

        void SetupAction(DiServiceProvider sp)
        {
            sp.AddSingletonT(new RenderConfigVars()
            {
                VariableDeclarations = "//This is super cool!\nx: 0,"
            });

            sp.AddSingletonT(new AlgoBalanced1Settings()
            {
                skipClassIndentation = skipIndentation,
            });
        }

        InputSmBuilder inputSmBuilder = new(SetupAction);
        inputSmBuilder.SetStateMachineRoot(sm);
        inputSmBuilder.FinishRunning();

        //NameMangler mangler = new();
        //mangler.SetStateMachine(sm);
        //StateMachineProvider stateMachineProvider = new(sm);
        //EnumBuilder enumBuilder = new(mangler, stateMachineProvider);
        //PseudoStateHandlerBuilder pseudoStateHandlerBuilder = new();
        //EventHandlerBuilder eventHandlerBuilder = new(new(), pseudoStateHandlerBuilder, mangler);

        AlgoBalanced1 builder = inputSmBuilder.sp.GetInstanceOf<AlgoBalanced1>();
        return builder.GenerateGil(sm);
    }
}

