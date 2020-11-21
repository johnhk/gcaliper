﻿/*******************************************************************************************************

  Copyright (C) Sebastian Loncar, Web: http://loncar.de
  Project: https://github.com/Arakis/gcaliper

  MIT License:

  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
  associated documentation files (the "Software"), to deal in the Software without restriction,
  including without limitation the rights to use, copy, modify, merge, publish, distribute,
  sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all copies or substantial
  portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
  NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
  OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
  CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*******************************************************************************************************/

using System;
using Gtk;
using Gdk;
using Cairo;
using POINT = System.Drawing.Point;
using RECT = System.Drawing.Rectangle;
using IO = System.IO;

namespace gcaliper
{
    public class TCaliperGroup : TDrawGroup
    {
        public TCaliperPartHead partHead;
        public TCaliperPartBottom partBottom;
        public TCaliperPartDisplay partDisplay;
        public TCaliperPartScale partScale;

        public TCaliperGroup()
        {
            loadTheme(System.IO.Path.Combine(themesRootDirectory, AppConfig.themeName));

            parts.Add(partBottom = new TCaliperPartBottom());
            parts.Add(partHead = new TCaliperPartHead());
            parts.Add(partDisplay = new TCaliperPartDisplay());
            parts.Add(partScale = new TCaliperPartScale());

            //part2.rect.Y = 20;
            distance = 100;
            partScale.rect.Location = scaleOffset;

            setContrastColor(jawColor);

            var color1 = new MenuItem("Color");
            menu.Insert(color1, 0);
            color1.ButtonReleaseEvent += (o, e) =>
            {
                if (e.Event.Button == 1)
                    showColorChooser();
            };

            // TODO: Place in visible area
            // TODO: respect DPI
            //setWindowPosition(new POINT(3000, 0));
        }

        public void showColorChooser()
        {
            using (var chooser = new ColorSelectionDialog("change color"))
            {
                //chooser.TransientFor=this;
                chooser.Style = originalStyle;

                if (chooser.Run() == (int)ResponseType.Ok)
                {
                    AppConfig.jawColor = new TColor(chooser.ColorSelection.CurrentColor);
                    AppConfig.save();
                    setContrastColor(AppConfig.jawColor);
                }
                chooser.Hide();
            }
        }

        public string themesRootDirectory
        {
            get
            {
                return IO.Path.Combine(AppConfig.appRootDir, "themes");
            }
        }

        public void loadTheme(string themeDir)
        {
            var themeFile = IO.Path.Combine(themeDir, "theme.conf");
            var ini = new INIFile(themeFile);
            rotationCenterImage = new POINT(ini.GetValue("theme", "rotationCenterX", 0), ini.GetValue("theme", "rotationCenterY", 0));
            displayCenterOffset = new POINT(ini.GetValue("theme", "displayCenterX", 0), ini.GetValue("theme", "displayCenterY", 0));
            scaleOffset = new POINT(ini.GetValue("theme", "scaleOffsetX", 0), ini.GetValue("theme", "scaleOffsetY", 0));
            ZeroDistanceOffset = ini.GetValue("theme", "zeroDistanceOffset", 0);
        }
        // *** configuration ***
        public POINT rotationCenterImage;
        // = new POINT (20, 65);
        public POINT scaleOffset;
        public POINT displayCenterOffset;
        // = new POINT (45, 68);
        public int ZeroDistanceOffset;
        // = 15;
        // ***
        public int minDistanceForRotation = 10;
        public double snapAngle = 0.5;
        private TColor jawColor = AppConfig.jawColor;
        double angle = 0.0174532925 * 0;
        static double DEG1 = 0.0174532925;
        // ***
        double tmpAngle = 0;
        public RECT unrotatedRect;
        public RECT rotatedRect;
        public POINT rotationCenterRoot;
        // = new POINT (1920 + 1920 / 2, 1200 / 2);
        public POINT rotationCenterZero = new POINT(0, 0);

        public void setContrastColor(TColor color)
        {
            jawColor = color;
            foreach (var part in parts)
            {
                part.applyContrast(color);
            }
            invalidateImage();
        }

        protected override bool OnConfigureEvent(EventConfigure evnt)
        {
            updateRotationCenter();
            ensureInitialDrawn();
            return base.OnConfigureEvent(evnt);
        }

        private void ensureInitialDrawn()
        {
            if (!positioned)
            {
                needRedraw = false;
                positioned = true;
                invalidateImage();
            }
        }

        protected override void OnShown()
        {
            updateRotationCenter();
            ensureInitialDrawn();
            base.OnShown();
        }

        public void generateImage()
        {
            unrotatedRect = parts.getRotationRect();

            using (var surf = new Cairo.ImageSurface(Format.ARGB32, unrotatedRect.Width, unrotatedRect.Height))
            {
                using (var cr = new Context(surf))
                {

                    //Clear
                    if (debug)
                    {
                        cr.SetSourceColor(new Cairo.Color(0, 0.9, 0));
                        cr.Rectangle(0, 0, unrotatedRect.Width, unrotatedRect.Height);
                        cr.Fill();
                    }
                    else
                    {
                        cr.Operator = Operator.Clear;
                        cr.Paint();
                        cr.Operator = Operator.Over;
                    }


                    foreach (var part in parts)
                    {
                        if (part.rotate)
                        {
                            //Draw image

                            part.draw(cr);
                        }
                    }

                    if (debug)
                    {
                        cr.LineWidth = 5;
                        cr.SetSourceRGBA(1, 0, 0, 1);
                        cr.Translate(debugPoint.X, debugPoint.Y);
                        cr.Arc(0, 0, 2, 0, Math.PI * 2);
                        cr.StrokePreserve();
                    }

                }

                //surf.WriteToPng ("test.png");

                //var angle = 0;
                var oldRotatedRect = rotatedRect;
                rotatedRect = funcs.rotateRect(unrotatedRect, rotationCenterZero, angle);

                //Rotate
                var surf2 = new Cairo.ImageSurface(Format.ARGB32, rotatedRect.Width, rotatedRect.Height);
                using (var cr = new Context(surf2))
                {
                    cr.Operator = Operator.Clear;
                    cr.Paint();
                    cr.Operator = Operator.Over;

                    cr.Translate(-rotatedRect.X, -rotatedRect.Y);
                    cr.Rotate(angle);
                    //var pp = funcs.rotatePoint (rotationRect.Location, new POINT (0, 0), angle);
                    using (var pat2 = new SurfacePattern(surf))
                    {
                        //pat2.Matrix = new Matrix (){ X0 =  -rr.X, Y0 = -rr.Y };

                        cr.SetSource(pat2);
                        //cr.Translate (100, 100);
                        cr.Paint();
                    }

                    //Debug
                    if (true)
                    {
                        cr.Matrix = new Matrix();
                        if (debugText != null)
                        {
                            //cr.Operator=Operator.Source;
                            cr.SetSourceRGBA(0, 1, 0, 1);
                            cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                            cr.SetFontSize(20);
                            cr.MoveTo(20, 20);
                            cr.ShowText(debugText);
                            cr.Fill();
                        }
                    }

                    foreach (var part in parts)
                    {
                        if (!part.rotate)
                        {
                            cr.Matrix = new Matrix();

                            var c = new POINT(partBottom.rect.Location.X + displayCenterOffset.X, partBottom.rect.Location.Y + displayCenterOffset.Y);

                            part.rect.X = c.X;
                            part.rect.Y = c.Y;

                            var p = ImagePosToRotatedPos(part.rect.Location);

                            p.X -= part.rect.Width / 2;
                            p.Y -= part.rect.Height / 2;

                            //Draw image

                            using (var pat = new SurfacePattern(part.image))
                            {
                                pat.Matrix = new Matrix() { X0 = -p.X, Y0 = -p.Y };
                                //pat.Matrix = pat.Matrix;

                                cr.SetSource(pat);
                                cr.Rectangle(new Cairo.Rectangle(p.X, p.Y, part.rect.Width, part.rect.Height));
                                cr.Fill();

                                cr.SetSourceRGBA(0, 0, 0, 1);
                                cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                                cr.SetFontSize(10);
                                cr.MoveTo(p.X + 12, p.Y + 27.2);
                                var text = distance.ToString();

                                cr.ShowText(text);

                                var deg = Math.Round(funcs.RadToDeg(angle));

                                if (deg % 45 != 0)
                                {
                                    cr.MoveTo(p.X + 14, p.Y + 40.2);
                                    text = deg.ToString() + "°";
                                    cr.ShowText(text);
                                }

                                cr.Fill();
                            }
                        }
                    }
                    /*
                                        cr.Matrix = new Matrix ();
                                        var pos = ImagePosToRotatedPos (part2.rect.Location);
                                        //cr.Translate (pos.X, pos.Y);

                                        cr.SetSourceRGBA (0, 1, 0, 1);
                                        cr.SelectFontFace ("Arial", FontSlant.Normal, FontWeight.Normal);
                                        cr.SetFontSize (10);
                                        cr.MoveTo (pos.X, pos.Y);
                                        cr.ShowText ("aaaa");
                                        cr.Fill ();
                    */
                }

                //surf2.WriteToPng ("test2.png");

                if (image != null)
                    image.Dispose();
                image = surf2;
            }
        }

        public int distance
        {
            get
            {
                return partBottom.rect.X - ZeroDistanceOffset;
            }
            set
            {
                if (distance == value)
                    return;
                value = Math.Max(value, 0);
                partBottom.rect.X = value + ZeroDistanceOffset;
                updatePartScale();
                invalidateImage();
            }
        }

        public bool debug = false;
        private string _debugText;

        public string debugText
        {
            get
            {
                return _debugText;

            }
            set
            {
                if (value == _debugText)
                    return;
                _debugText = value;
                invalidateImage();
            }
        }

        private POINT rootMousePos;
        private POINT mousePos;
        private POINT startRootMousePos;
        private POINT startRectPos;
        private POINT startWinPos;
        private POINT mouseImagePos;
        private bool resizing = false;
        private bool moving = false;
        private POINT debugPoint = new POINT(10, 10);
        private double moveMouseAngleOffset;
        private int moveMouseXOffset;

        private POINT AbsPosToUnrotatedPos(POINT pos)
        {
            return funcs.rotatePoint(new POINT(mousePos.X + rotatedRect.X, mousePos.Y + rotatedRect.Y), new POINT(0, 0), -angle);
        }

        public int getDistanceToRotationCenter(POINT rootPos)
        {
            //return (int)Math.Round (Math.Abs (Math.Sqrt (Math.Pow (rootPos.X - rotationCenterRoot.X, 2) + Math.Pow (rootPos.Y - rotationCenterRoot.Y, 2))));
            //rotationCenterImage.X

            int x, y;
            GetPosition(out x, out y);

            var p = AbsPosToUnrotatedPos(new POINT(rootPos.X - x, rootPos.Y - y));
            return p.X - rotationCenterImage.X;
        }

        protected override bool OnMotionNotifyEvent(EventMotion evnt)
        {
            rootMousePos = new POINT((int)evnt.XRoot, (int)evnt.YRoot);
            mousePos = new POINT((int)evnt.X, (int)evnt.Y);

            mouseImagePos = AbsPosToUnrotatedPos(mousePos);

            if (debug)
            {
                debugText = partBottom.rect.Contains(mouseImagePos).ToString();
                debugPoint = mouseImagePos;
                invalidateImage();
            }

            var relMousePos = new POINT(rootMousePos.X - startRootMousePos.X, rootMousePos.Y - startRootMousePos.Y);

            if (resizing)
            {
                if (Math.Abs(relMousePos.X) > 10 || Math.Abs(relMousePos.Y) > 10)
                {

                    partBottom.rect.X = getDistanceToRotationCenter(rootMousePos);
                    partBottom.rect.X -= moveMouseXOffset;
                    partBottom.rect.X = Math.Max(partBottom.rect.X, ZeroDistanceOffset);
                    updatePartScale();

                    if (distance > minDistanceForRotation)
                    {
                        tmpAngle = funcs.GetAngleOfLineBetweenTwoPoints(rotationCenterRoot, rootMousePos);
                        tmpAngle -= moveMouseAngleOffset;
                        tmpAngle = normalizeAngle(tmpAngle);

                        if ((evnt.State & ModifierType.ControlMask) == ModifierType.ControlMask && (evnt.State & ModifierType.ShiftMask) != ModifierType.ShiftMask)
                        {
                            angle = tmpAngle;
                        }
                        else
                        {
                            var snapAngle = this.snapAngle;
                            double[] angleMarkers;
                            if ((evnt.State & ModifierType.ShiftMask) == ModifierType.ShiftMask)
                            {
                                angleMarkers = new double[] {
                                    0,
                                    Math.PI / 4,
                                    Math.PI / 2,
                                    Math.PI,
                                    Math.PI - Math.PI / 4,
                                    -(Math.PI - Math.PI / 4),
                                    -(Math.PI / 2),
                                    -(Math.PI / 4)
                                };
                                snapAngle = snapAngle / 2;
                            }
                            else
                                angleMarkers = new double[] { 0, Math.PI / 2, Math.PI, -Math.PI, -(Math.PI / 2) };

                            for (var i = 0; i < angleMarkers.Length; i++)
                            {
                                var a = angleMarkers[i];
                                if (tmpAngle <= a + snapAngle && tmpAngle >= a - snapAngle)
                                {
                                    setAngle(a);
                                    break;
                                }
                            }
                        }
                    }

                    invalidateImage();
                }
            }

            if (moving)
            {
                var x = (startWinPos.X + (rootMousePos.X - startRootMousePos.X));
                var y = (startWinPos.Y + (rootMousePos.Y - startRootMousePos.Y));
                Move(x, y);
                updateRotationCenter();
            }

            return base.OnMotionNotifyEvent(evnt);
        }

        public double normalizeAngle(double angle)
        {
            if (angle >= Math.PI)
                angle -= Math.PI * 2;
            if (angle <= -Math.PI)
                angle += Math.PI * 2;
            return angle;
        }

        public void setAngle(double angle)
        {
            this.angle = normalizeAngle(angle);
            invalidateImage();
        }

        private void updatePartScale()
        {
            partScale.rect.Width = distance;
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            mousePos = new POINT((int)evnt.X, (int)evnt.Y);
            mouseImagePos = AbsPosToUnrotatedPos(mousePos);
            if (evnt.Button == 1)
            {
                int x;
                int y;
                GetPosition(out x, out y);

                startWinPos = new POINT(x, y);
                startRootMousePos = new POINT((int)evnt.XRoot, (int)evnt.YRoot);
                startRectPos = partBottom.rect.Location;

                if (partBottom.rect.Contains(mouseImagePos))
                {
                    resizing = true;

                    moveMouseXOffset = getDistanceToRotationCenter(startRootMousePos) - partBottom.rect.X;
                    moveMouseAngleOffset = funcs.GetAngleOfLineBetweenTwoPoints(rotationCenterRoot, startRootMousePos) - angle;

                }
                else if (partHead.rect.Contains(mouseImagePos) || partScale.rect.Contains(mouseImagePos))
                {
                    moving = true;
                }
            }
            if (evnt.Button == 3)
            {
                this.menu.ShowAll();
                this.menu.Popup();
            }

            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnKeyPressEvent(EventKey e)
        {

            if (e.Key == Gdk.Key.Left || e.Key == Gdk.Key.Right || e.Key == Gdk.Key.Up || e.Key == Gdk.Key.Down)
            {
                var step = 1;
                var stepY = 0;

                if ((e.State & ModifierType.ShiftMask) == ModifierType.ShiftMask)
                    step = 20;

                if (e.Key == Gdk.Key.Left || e.Key == Gdk.Key.Up)
                    step = -step;

                if ((e.State & ModifierType.ControlMask) == ModifierType.ControlMask)
                {
                    distance += step;
                }
                else
                {
                    if (e.Key == Gdk.Key.Up || e.Key == Gdk.Key.Down)
                    {
                        stepY = step;
                        step = 0;
                    }

                    setWindowPosition(getWindowPosition().add(step, stepY));
                }
            }

            if (e.Key == Gdk.Key.r || e.Key == Gdk.Key.t || e.Key == Gdk.Key.R || e.Key == Gdk.Key.T)
            {
                var angleDistance = Math.PI / 2;
                if ((e.State & ModifierType.ShiftMask) == ModifierType.ShiftMask)
                {
                    angleDistance = Math.PI / 4;
                }
                if ((e.State & ModifierType.ControlMask) == ModifierType.ControlMask)
                {
                    angleDistance = DEG1;
                }

                if (e.Key == Gdk.Key.t || e.Key == Gdk.Key.T)
                {
                    angleDistance = -angleDistance;
                }

                setAngle(angle - angleDistance);
            }

            if (e.Key == Gdk.Key.v)
            {
                setAngle(Math.PI / 2);
            }
            if (e.Key == Gdk.Key.h)
            {
                setAngle(0);
            }

            if (e.Key == Gdk.Key.n)
            {
                Iconify();
            }

            if (e.Key == Gdk.Key.Home)
            {
                distance = 0;
            }

            if (e.Key == Gdk.Key.End)
            {
                var mon = Screen.GetMonitorAtWindow(GdkWindow);
                var geo = Screen.GetMonitorGeometry(mon);
                if (angle == 0)
                    distance = geo.Width - 200;
                else
                    distance = geo.Height - 200;
            }

            if (e.Key == Gdk.Key.c)
            {
                showColorChooser();
            }

            if ((e.Key == Gdk.Key.q || e.Key == Gdk.Key.w) && (e.State & ModifierType.ControlMask) == ModifierType.ControlMask)
            {
                Application.Quit();
            }

            return base.OnKeyPressEvent(e);
        }

        private POINT getWindowPosition()
        {
            int x, y;
            GetPosition(out x, out y);
            return new POINT(x, y);
        }

        private void setWindowPosition(POINT p)
        {
            Move(p.X, p.Y);
        }

        protected override bool OnButtonReleaseEvent(EventButton evnt)
        {
            if (evnt.Button == 1)
            {
                resizing = false;
                moving = false;
            }
            return base.OnButtonReleaseEvent(evnt);
        }

        private Surface bgPixMap;

        public void updatePosition()
        {
            var p = rotationCenterRoot;

            var r = funcs.rotatePoint(rotationCenterImage, rotationCenterZero, angle);

            p.X -= r.X;
            p.Y -= r.Y;

            p.X += rotatedRect.X;
            p.Y += rotatedRect.Y;

            Move(p.X, p.Y);
        }

        public void updateRotationCenter()
        {
            //return;
            int x, y;
            GetPosition(out x, out y);

            var p = new POINT(x, y);
            var r = funcs.rotatePoint(rotationCenterImage, rotationCenterZero, angle);

            p.X -= rotatedRect.X;
            p.Y -= rotatedRect.Y;

            p.X += r.X;
            p.Y += r.Y;

            rotationCenterRoot = p;
        }

        public POINT ImagePosToRotatedPos(POINT imgPos)
        {
            var p = new POINT(0, 0);
            var r = funcs.rotatePoint(imgPos, rotationCenterZero, angle);

            p.X -= rotatedRect.X;
            p.Y -= rotatedRect.Y;

            p.X += r.X;
            p.Y += r.Y;

            return p;
        }

        public bool positioned = false;

        public void drawImage(Context cr)
        {
            if (image != null)
            {
                cr.SetSource(image);
                cr.Rectangle(0, 0, image.Width, image.Height);
                cr.Fill();
            }
        }

        public void redraw(Context cr)
        {
            //to avoid flickering
            drawImage(cr);

            if (!positioned)
                return;

            needRedraw = false;
            try
            {

                generateImage();
                generateMask();

                //sizing window bigger than screen is requied, because some window managers does not allow positioning windows outside the screen, when they fit
                SetSizeRequest(Math.Max(image.Width, this.Screen.Width + 10), image.Height);

                setWindowShape();

                drawImage(cr);

                updatePosition();
            }
            catch (Exception ex)
            {
                new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, ex.ToString());
            }

        }

        protected override bool OnDrawn(Context cr)
        {
            redraw(cr);
            return true;
        }

        //        protected override bool OnExposeEvent(EventExpose evnt)
        //        {
        //            if (needRedraw)
        //                redraw();
        //
        //            return base.OnExposeEvent(evnt);
        //        }
    }
}