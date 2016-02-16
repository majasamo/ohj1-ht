using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class Konetus : PhysicsGame
{
    public override void Begin()
    {
        Level.CreateBorders();

        PhysicsObject kone = new PhysicsObject(100, 30, Shape.Rectangle);
        kone.Color = Color.Orange;
        kone.LinearDamping = 0.7; // Määritellään, kuinka nopeasti koneen vauhti hidastuu.
        kone.Restitution = 0.3; // Määritellään koneen kimmoisuus.
        kone.MomentOfInertia = 100; // Koneen hitausmomentti.
        kone.AngularDamping = 0.6; // Määritellään, kuinka nopeasti koneen kulmanopeus pienenee.
        Add(kone);

        Keyboard.Listen(Key.Up, ButtonState.Down, LiikutaEteen, "Liikuta konetta eteenpäin", kone, 3000.0);
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaTaakse, "Liikuta konetta taaksepäin", kone, 560.0);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanna, "Käännä konetta vasemmalle", kone, 2000.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanna, "Käännä konetta oikealle", kone, -2000.0);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohje");


        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    public void LiikutaEteen(PhysicsObject liikutettava, double vauhti)
    {
        Vector kulkusuunta = Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kulkusuunta);
    }

    public void LiikutaTaakse(PhysicsObject liikutettava, double vauhti)
    {
        Vector kaanteinenKulkusuunta = - Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kaanteinenKulkusuunta);
    }

    public void Kaanna(PhysicsObject kaannettava, double vaantovoima)
    {
        kaannettava.ApplyTorque(vaantovoima);
    }
}
