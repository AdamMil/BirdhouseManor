using System;
using System.Drawing;
using System.IO;
using GameLib.Events;
using GameLib.Input;
using GameLib.Interop.OpenGL;
using GameLib.Video;

namespace BirdhouseManor
{

sealed class SDLUserInterface
{
  public void Run()
  {
    Game game = new Game(Path.Combine(Program.DataPath, "games/CastleRavenloft/game.xml"));

    try
    {
      Events.Initialize();
      Input.Initialize();
      Video.Initialize();
      SetVideoMode();
      Events.PumpEvents(HandleEvent);
    }
    finally
    {
      if(Video.Initialized) Video.Deinitialize();
      if(Input.Initialized) Input.Deinitialize();
      if(Events.Initialized) Events.Deinitialize();
    }
  }

  static bool HandleEvent(Event e)
  {
    return e.Type != EventType.Quit;
  }

  static void ResetOpenGL()
  {
    // everything is parallel to the screen, so perspective correction is not necessary
    GL.glHint(GL.GL_PERSPECTIVE_CORRECTION_HINT, GL.GL_FASTEST);

    GL.glHint(GL.GL_LINE_SMOOTH_HINT, GL.GL_NICEST); // these look nice (but smoothing is disabled by default)
    GL.glHint(GL.GL_POLYGON_SMOOTH_HINT, GL.GL_NICEST);

    GL.glDisable(GL.GL_DEPTH_TEST);     // typically, we're drawing in order, so this isn't needed.
    GL.glDisable(GL.GL_CULL_FACE);      // everything is forward-facing, so we don't need culling.
    GL.glDisable(GL.GL_BLEND);          // things that need blending will enable it.
    GL.glDisable(GL.GL_LINE_SMOOTH);    // smoothing requires blending, so the rule above applies.
    GL.glDisable(GL.GL_POLYGON_SMOOTH); // ditto
    GL.glDisable(GL.GL_TEXTURE_2D);     // things that need texturing will enable it.
    GL.glDisable(GL.GL_LIGHTING);       // things that need lighting will enable it.
    GL.glDisable(GL.GL_DITHER);         // don't need this.
    GL.glClearColor(0, 0, 0, 0);        // clear to black by default.
    GL.glLineWidth(1);                  // lines will be one pixel wide.
    GL.glPointSize(1);                  // pixels, too.
    GL.glShadeModel(GL.GL_FLAT);        // use flat shading by default
    GL.glBlendFunc(GL.GL_ONE, GL.GL_ZERO); // use the default blend mode

    GL.glBindTexture(GL.GL_TEXTURE_2D, 0); // unbind any bound texture

    GL.glMatrixMode(GL.GL_PROJECTION);
    GL.glLoadIdentity();
    GLU.gluOrtho2D(0, Video.Width, Video.Height, 0);

    GL.glMatrixMode(GL.GL_MODELVIEW);
    GL.glLoadIdentity();
    GL.glTranslated(0.375, 0.375, 0); // opengl "exact pixelization" hack described in OpenGL Redbook appendix H

    SetViewport(new Rectangle(0, 0, Video.Width, Video.Height));
  }

  static void SetVideoMode()
  {
    Video.SetGLMode(800, 600, 0);
    WM.WindowTitle = "Birdhouse Manor";
    ResetOpenGL();
  }

  static void SetViewport(Rectangle viewport)
  {
    GL.glViewport(viewport.X, Video.Height-viewport.Bottom, viewport.Width, viewport.Height);
  }
}

} // namespace BirdhouseManor
