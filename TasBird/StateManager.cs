using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class StateManager : MonoBehaviour
    {
        public static Dictionary<string, State> States { get; } = new Dictionary<string, State>();

        private StateManager()
        {
            Util.SceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            Util.SceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded()
        {
            States.Clear();
        }

        private void Update()
        {
            foreach (var keyChar in "1234567890")
            {
                var key = keyChar.ToString();
                if (!Input.GetKeyDown(key))
                    continue;

                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    var state = State.Save();
                    if (state.HasValue)
                        States[key] = state.Value;
                }
                else
                {
                    if (States.ContainsKey(key))
                        States[key].Load();
                }
            }
        }
    }

    public struct State
    {
        private TimeState time;
        private PlayerState player;
        private PlayerPipState playerPip;
        private CameraState camera;
        private InputState input;
        private FaderState fader;
        private BossState? boss;

        public uint Frame => time.Frame;

        public static State? Save()
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return null;

            return new State
            {
                time = new TimeState(),
                player = new PlayerState(),
                playerPip = new PlayerPipState(),
                camera = new CameraState(),
                input = new InputState(),
                fader = new FaderState(),
                boss = BossState.Save()
            };
        }

        public void Load()
        {
            time.Load();
            player.Load();
            playerPip.Load();
            camera.Load();
            input.Load();
            fader.Load();
            boss?.Load();

            // NOTE: Hacks to facilitate boss TASing.
            if (boss != null)
            {
                // Normally the opening cutscene handles unlocking the player,
                // but if you save a state before the cutscene ends, then the
                // player will never get unlocked.
                MasterController.input.locked = false;
            }
        }

        public bool IsPrefixOf(ReplayData buffers) => input.IsPrefixOf(buffers);

        private class TimeState
        {
            private readonly uint frame;
            private readonly bool paused;
            private readonly float multiplier;

            public uint Frame => frame;

            public TimeState()
            {
                frame = Time.Frame;
                paused = Time.Paused;
                multiplier = Time.Multiplier;
            }

            public void Load()
            {
                Time.Frame = frame;
                Time.Paused = paused;
                Time.Multiplier = multiplier;
            }
        }

        private class PlayerState
        {
            private readonly Vector contact;
            private readonly Coord gravity;
            private readonly IShape hitbox;
            private readonly List<Vector> multiContact;
            private readonly Coord position;
            private readonly Coord prevPosition;
            private readonly Coord velocity;

            private readonly Player.CheckpointState checkpointState;
            private readonly Player.CloakState cloak;
            private readonly bool cloakSound;
            private readonly float cloakVolume;
            private readonly float cutscenePoint;
            private readonly Player.DashState dash;
            private readonly int deathCount;
            private readonly bool ending;
            private readonly bool facingRight;
            private readonly int framesInLevel;
            private readonly bool isDashing;
            private readonly Player.JumpState jump;
            private readonly bool killed;
            private readonly Vector lastDash;
            private readonly Player.LedgeState ledgeState;
            private readonly bool movingToCutsceneStart;
            private readonly CutsceneTrigger repeatableTrigger;
            private readonly GameObject repeateableCutscene;
            private readonly SFX slideSource;
            private readonly bool startingCutscene;
            private readonly Player.Timer timers;

            public PlayerState()
            {
                var player = MasterController.GetPlayer();

                contact = player.contact;
                gravity = player.gravity;
                hitbox = Clone(player.hitbox);
                multiContact = Clone(player.multiContact);
                position = player.position;
                prevPosition = player.prevPosition;
                velocity = player.velocity;

                checkpointState = Clone(player.checkpointState);
                cloak = Clone(player.cloak);
                cloakSound = player.cloakSound;
                cloakVolume = player.cloakVolume;
                cutscenePoint = player.cutscenePoint;
                dash = Clone(player.dash);
                deathCount = player.deathCount;
                ending = player.ending;
                facingRight = player.facingRight;
                framesInLevel = player.framesInLevel;
                isDashing = player.isDashing;
                jump = Clone(player.jump);
                killed = player.killed;
                lastDash = player.lastDash;
                ledgeState = player.ledgeState;
                movingToCutsceneStart = player.movingToCutsceneStart;
                repeatableTrigger = player.repeatableTrigger;
                repeateableCutscene = player.repeateableCutscene;
                slideSource = player.slideSource;
                startingCutscene = player.startingCutscene;
                timers = Clone(player.timers);
            }

            public void Load()
            {
                var player = MasterController.GetPlayer();

                player.contact = contact;
                player.gravity = gravity;
                player.hitbox = Clone(hitbox);
                player.multiContact = Clone(multiContact);
                player.position = position;
                player.prevPosition = prevPosition;
                player.velocity = velocity;

                player.checkpointState = Clone(checkpointState);
                player.cloak = Clone(cloak);
                player.cloakSound = cloakSound;
                player.cloakVolume = cloakVolume;
                player.cutscenePoint = cutscenePoint;
                player.dash = Clone(dash);
                player.deathCount = deathCount;
                player.ending = ending;
                player.facingRight = facingRight;
                player.framesInLevel = framesInLevel;
                player.isDashing = isDashing;
                player.jump = Clone(jump);
                player.killed = killed;
                player.lastDash = lastDash;
                player.ledgeState = ledgeState;
                player.movingToCutsceneStart = movingToCutsceneStart;
                player.repeatableTrigger = repeatableTrigger;
                player.repeateableCutscene = repeateableCutscene;
                player.slideSource = slideSource;
                player.startingCutscene = startingCutscene;
                player.timers = Clone(timers);
            }
        }

        private class PlayerPipState
        {
            private readonly int countdown;

            public PlayerPipState()
            {
                var playerPip = PlayerPip.Instance;

                countdown = playerPip.countdown;
            }

            public void Load()
            {
                var playerPip = PlayerPip.Instance;

                playerPip.countdown = countdown;
            }
        }

        private class CameraState
        {
            private readonly Coord position;

            public CameraState()
            {
                var camera = MasterController.GetCamera();

                position = camera.state.Position;
            }

            public void Load()
            {
                var camera = MasterController.GetCamera();

                camera.ForcePosition(position);
            }
        }

        private class InputState
        {
            private readonly Dictionary<InputManager.Axis, InputManager.AxisChannel> axes;
            private readonly Dictionary<InputManager.Key, InputManager.ButtonChannel> buttons;
            private readonly bool isChanging;
            private readonly bool isReplay;
            private readonly bool locked;
            private readonly uint timeCount;
            private readonly bool updateReady;

            public InputState()
            {
                var input = MasterController.GetInput();

                axes = Clone(input.axes);
                buttons = Clone(input.buttons);
                isChanging = input.isChanging;
                isReplay = input.isReplay;
                locked = input.locked;
                timeCount = input.timeCount;
                updateReady = input.updateReady;
            }

            public void Load()
            {
                var input = MasterController.GetInput();

                input.axes = Clone(axes);
                input.buttons = Clone(buttons);
                input.isChanging = isChanging;
                input.isReplay = isReplay;
                input.locked = locked;
                input.timeCount = timeCount;
                input.updateReady = updateReady;
            }

            public bool IsPrefixOf(ReplayData buffers)
            {
                foreach (var axis in axes)
                {
                    var axisBuffer = axis.Value.buffer;
                    var i = 0;
                    foreach (var entry in buffers.axisBuffers[(int)axis.Key])
                    {
                        if (i >= axisBuffer.Count)
                        {
                            if (entry.Key <= timeCount)
                            {
                                // Replay buffer has an input that is not present in our inputs
                                return false;
                            }
                        }
                        else if (entry.Key != axisBuffer[i].time || entry.Value != (int)axisBuffer[i].state)
                        {
                            // There is a mismatched entry
                            return false;
                        }

                        i += 1;
                    }
                }

                foreach (var button in buttons)
                {
                    var buttonBuffer = button.Value.buffer;
                    var i = 0;
                    foreach (var entry in buffers.buttonBuffers[(int)button.Key])
                    {
                        if (i >= buttonBuffer.Count)
                        {
                            if (entry.Key <= timeCount)
                            {
                                // Replay buffer has an input that is not present in our inputs
                                return false;
                            }
                        }
                        else if (entry.Key != buttonBuffer[i].time || entry.Value != (int)buttonBuffer[i].state)
                        {
                            // There is a mismatched entry
                            return false;
                        }
                        i += 1;
                    }
                }

                return true;
            }
        }

        private class FaderState
        {
            private event EventHandler<EventArgs> FadeBackEvent;
            private event EventHandler<EventArgs> FadeOutEvent;
            private readonly bool autoFadeBack;
            private readonly float currentFrameCount;
            private readonly float fadeDuration;
            private readonly float fadeOrigin;
            private readonly float fadeTarget;
            private readonly bool fading;
            private readonly Color faderColor;
            private readonly string nextLevel;

            public FaderState()
            {
                var fader = GameObject.Find("Fader").GetComponent<FadeLoader>();

                FadeBackEvent = AccessTools.FieldRefAccess<FadeLoader, EventHandler<EventArgs>>(fader, "FadeBackEvent");
                FadeOutEvent = AccessTools.FieldRefAccess<FadeLoader, EventHandler<EventArgs>>(fader, "FadeOutEvent");
                autoFadeBack = fader.autoFadeBack;
                currentFrameCount = fader.currentFrameCount;
                fadeDuration = fader.fadeDuration;
                fadeOrigin = fader.fadeOrigin;
                fadeTarget = fader.fadeTarget;
                fading = fader.fading;
                faderColor = fader.fader.color;
                nextLevel = fader.nextLevel;
            }

            public void Load()
            {
                var fader = GameObject.Find("Fader").GetComponent<FadeLoader>();

                AccessTools.FieldRefAccess<FadeLoader, EventHandler<EventArgs>>(fader, "FadeBackEvent") = FadeBackEvent;
                AccessTools.FieldRefAccess<FadeLoader, EventHandler<EventArgs>>(fader, "FadeOutEvent") = FadeOutEvent;
                fader.autoFadeBack = autoFadeBack;
                fader.currentFrameCount = currentFrameCount;
                fader.fadeDuration = fadeDuration;
                fader.fadeOrigin = fadeOrigin;
                fader.fadeTarget = fadeTarget;
                fader.fading = fading;
                fader.fader.color = faderColor;
                fader.nextLevel = nextLevel;
            }
        }

        private struct BossState
        {
            private readonly Vector contact;
            private readonly Coord gravity;
            private readonly IShape hitbox;
            private readonly List<Vector> multiContact;
            private readonly Coord position;
            private readonly Coord prevPosition;
            private readonly Coord velocity;

            private readonly BossfightController.Timers timers;
            private readonly int health;
            private readonly int phase;
            private readonly int flyTo;
            private readonly FlyPoint flyTarget;
            private readonly float flyTime;
            private readonly SlamPoint targetSlam;
            private readonly bool flyingToSlam;
            private readonly SlamPoint trySlam;

            private readonly Dictionary<string, FightReceptacleState> fightReceptacles;

            public static BossState? Save()
            {
                var boss = MasterController.player.refs.boss;
                if (boss is null)
                    return null;
                return new BossState(boss);
            }

            private BossState(BossfightController boss)
            {
                contact = boss.contact;
                gravity = boss.gravity;
                hitbox = Clone(boss.hitbox);
                multiContact = Clone(boss.multiContact);
                position = boss.position;
                prevPosition = boss.prevPosition;
                velocity = boss.velocity;

                timers = Clone(boss.timers);
                health = boss.health;
                phase = boss.phase;
                flyTo = boss.flyTo;
                flyTarget = boss.flyTarget;
                flyTime = boss.flyTime;
                targetSlam = boss.targetSlam;
                flyingToSlam = boss.flyingToSlam;
                trySlam = boss.trySlam;

                // Save fight receptacles
                fightReceptacles = new Dictionary<string, FightReceptacleState>();
                foreach (var receptacle in MasterController.GetObjects().GetObjects<FightReceptacle>())
                    fightReceptacles[receptacle.gameObject.name] = new FightReceptacleState(receptacle);
            }

            public void Load()
            {
                var boss = MasterController.player.refs.boss;
                if (boss is null)
                    return;

                // Delete any existing projectiles
                boss.ResetRotation(false, true, false, false);

                boss.contact = contact;
                boss.gravity = gravity;
                boss.hitbox = Clone(hitbox);
                boss.multiContact = Clone(multiContact);
                boss.position = position;
                boss.prevPosition = prevPosition;
                boss.velocity = velocity;

                boss.timers = Clone(timers);
                boss.health = health;
                boss.phase = phase;
                boss.flyTo = flyTo;
                boss.flyTarget = flyTarget;
                boss.flyTime = flyTime;
                boss.targetSlam = targetSlam;
                boss.flyingToSlam = flyingToSlam;
                boss.trySlam = trySlam;

                // Reset fight receptacles
                foreach (var receptacle in MasterController.GetObjects().GetObjects<FightReceptacle>())
                    fightReceptacles[receptacle.gameObject.name].Load(receptacle);
            }
            private struct FightReceptacleState
            {
                private readonly int count;
                private readonly bool available;
                private readonly int timer;
                public FightReceptacleState(FightReceptacle receptacle)
                {
                    count = receptacle.Count;
                    available = receptacle._available;
                    timer = receptacle.timer;
                }

                public void Load(FightReceptacle receptacle)
                {
                    receptacle.Count = count;
                    receptacle._available = available;
                    receptacle.timer = timer;
                }
            }
        }


        private static List<Vector> Clone(List<Vector> other)
        {
            return new List<Vector>(other);
        }

        private static Player.Timer Clone(Player.Timer other)
        {
            return new Player.Timer
            {
                timers = Clone(other.timers),
                actions = Clone(other.actions)
            };
        }

        private static int[] Clone(int[] other)
        {
            return other.Clone() as int[];
        }

        private static Action[] Clone(Action[] other)
        {
            return other.Clone() as Action[];
        }

        private static Player.CheckpointState Clone(Player.CheckpointState other)
        {
            return new Player.CheckpointState
            {
                checkpoint = other.checkpoint, // Reference to a game object - don't clone
                cloakCount = other.cloakCount,
                cloakHidden = other.cloakHidden,
                colorRamp = other.colorRamp,
                contact = other.contact,
                overrideFollow = other.overrideFollow, // No idea what this is for
                position = other.position,
                ResetTime = other.ResetTime,
                wantToReset = other.wantToReset
            };
        }

        private static Player.DashState Clone(Player.DashState other)
        {
            return new Player.DashState
            {
                contact = other.contact,
                direction = other.direction,
                intent = other.intent
            };
        }

        private static Player.JumpState Clone(Player.JumpState other)
        {
            return new Player.JumpState
            {
                contact = other.contact,
                old = other.old
            };
        }

        private static Player.CloakState Clone(Player.CloakState other)
        {
            return new Player.CloakState
            {
                on = other.on,
                power = other.power,
                leftZone = other.leftZone,
                rightZone = other.rightZone,
                count = other.count
            };
        }

        private static IShape Clone(IShape other)
        {
            switch (other)
            {
                case Circle circle:
                    return new Circle
                    {
                        center = circle.center,
                        radius = circle.radius
                    };
                case Rectangle rectangle:
                    return new Rectangle
                    {
                        center = rectangle.center,
                        height = rectangle.height,
                        width = rectangle.width
                    };
                default:
                    return null;
            }
        }

        private static Dictionary<InputManager.Axis, InputManager.AxisChannel> Clone(
            Dictionary<InputManager.Axis, InputManager.AxisChannel> other)
        {
            return other.ToDictionary(entry => entry.Key, entry => Clone(entry.Value));
        }

        private static InputManager.AxisChannel Clone(InputManager.AxisChannel other)
        {
            switch (other)
            {
                case InputManager.AxisReplay replay:
                    return new InputManager.AxisReplay(null)
                    {
                        buffer = Clone(replay.buffer),
                        negative = replay.negative, // Don't bother cloning bindings
                        positive = replay.positive,
                        used = replay.used,
                        pointer = replay.pointer,
                        replay = Clone(replay.replay)
                    };
                default:
                    return new InputManager.AxisChannel
                    {
                        buffer = Clone(other.buffer),
                        negative = other.negative, // Don't bother cloning bindings
                        positive = other.positive,
                        used = other.used
                    };
            }
        }

        private static List<RootInputManager.Entry<InputManager.AxisChannel.State>> Clone(
            List<RootInputManager.Entry<InputManager.AxisChannel.State>> other)
        {
            // `Entry`s are readonly
            return new List<RootInputManager.Entry<InputManager.AxisChannel.State>>(other);
        }

        private static Dictionary<InputManager.Key, InputManager.ButtonChannel> Clone(
            Dictionary<InputManager.Key, InputManager.ButtonChannel> other)
        {
            return other.ToDictionary(entry => entry.Key, entry => Clone(entry.Value));
        }

        private static InputManager.ButtonChannel Clone(InputManager.ButtonChannel other)
        {
            switch (other)
            {
                case InputManager.ButtonReplay replay:
                    return new InputManager.ButtonReplay(null)
                    {
                        buffer = Clone(replay.buffer),
                        binding = replay.binding, // Don't bother cloning binding
                        used = replay.used,
                        pointer = replay.pointer,
                        replay = Clone(replay.replay)
                    };
                default:
                    return new InputManager.ButtonChannel
                    {
                        buffer = Clone(other.buffer),
                        binding = other.binding, // Don't bother cloning binding
                        used = other.used
                    };
            }
        }

        private static List<RootInputManager.Entry<InputManager.ButtonChannel.State>> Clone(
            List<RootInputManager.Entry<InputManager.ButtonChannel.State>> other)
        {
            // `Entry`s are readonly
            return new List<RootInputManager.Entry<InputManager.ButtonChannel.State>>(other);
        }

        private static BossfightController.Timers Clone(BossfightController.Timers other)
        {
            var timers = new BossfightController.Timers();
            other.ForEach(timer => timers.Add(Clone(timer)));
            return timers;
        }

        private static BossfightController.Timer Clone(BossfightController.Timer other)
        {
            return new BossfightController.Timer
            {

                time = other.time,
                action = other.action,
                done = other.done,
            };
        }
    }
}
