// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Graphics.UserInterface
{
    public class FPSCounter : CompositeDrawable, IHasCustomTooltip
    {
        private RollingCounter<double> msCounter = null!;
        private RollingCounter<double> fpsCounter = null!;

        private Container mainContent = null!;

        public FPSCounter()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChildren = new Drawable[]
            {
                mainContent = new Container
                {
                    Alpha = 0,
                    Size = new Vector2(30),
                    Children = new Drawable[]
                    {
                        msCounter = new FrameTimeCounter
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Colour = colours.Orange2,
                        },
                        fpsCounter = new FramesPerSecondCounter
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Y = 11,
                            Scale = new Vector2(0.8f),
                            Colour = colours.Lime3,
                        }
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            displayTemporarily();
        }

        private bool isDisplayed;

        private ScheduledDelegate? fadeOutDelegate;

        private void displayTemporarily()
        {
            if (!isDisplayed)
                mainContent.FadeTo(1, 300, Easing.OutQuint);

            fadeOutDelegate?.Cancel();
            fadeOutDelegate = Scheduler.AddDelayed(() =>
            {
                mainContent.FadeTo(0, 1000, Easing.In);
                isDisplayed = false;
            }, 2000);
        }

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        protected override void Update()
        {
            base.Update();

            // TODO: this is wrong (elapsed clock time, not actual run time).
            double newFrameTime = gameHost.UpdateThread.Clock.ElapsedFrameTime;
            double newFps = gameHost.DrawThread.Clock.FramesPerSecond;

            bool hasSignificantChanges =
                Math.Abs(msCounter.Current.Value - newFrameTime) > 5 ||
                Math.Abs(fpsCounter.Current.Value - newFps) > 10;

            if (hasSignificantChanges)
                displayTemporarily();

            msCounter.Current.Value = newFrameTime;
            fpsCounter.Current.Value = newFps;
        }

        public ITooltip GetCustomTooltip() => new FPSCounterTooltip();

        public object TooltipContent => this;

        public class FramesPerSecondCounter : RollingCounter<double>
        {
            protected override double RollingDuration => 400;

            protected override OsuSpriteText CreateSpriteText()
            {
                return new OsuSpriteText
                {
                    Font = OsuFont.Default.With(fixedWidth: true, size: 16, weight: FontWeight.SemiBold),
                    Spacing = new Vector2(-2),
                };
            }

            protected override LocalisableString FormatCount(double count)
            {
                return $"{count:#,0}fps";
            }
        }

        public class FrameTimeCounter : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override OsuSpriteText CreateSpriteText()
            {
                return new OsuSpriteText
                {
                    Font = OsuFont.Default.With(fixedWidth: true, size: 16, weight: FontWeight.SemiBold),
                    Spacing = new Vector2(-1),
                };
            }

            protected override LocalisableString FormatCount(double count)
            {
                if (count < 1)
                    return $"{count:N1}ms";

                return $"{count:N0}ms";
            }
        }
    }
}
