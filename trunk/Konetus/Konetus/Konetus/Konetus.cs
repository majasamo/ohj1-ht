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
        //Level.CreateBorders();


        PhysicsObject kone = new PhysicsObject(90.0, 40.0, Shape.Rectangle);
        kone.Position = new Vector(Level.Left, 0);
        kone.Color = Color.Orange;
        kone.LinearDamping = 0.7;  // Määritellään, kuinka nopeasti koneen vauhti hidastuu.
        kone.Restitution = 0.3;  // Määritellään koneen kimmoisuus.
        kone.MomentOfInertia = 100;  // Koneen hitausmomentti.
        kone.AngularDamping = 0.6;  // Määritellään, kuinka nopeasti koneen kulmanopeus pienenee.
        Add(kone);

        PhysicsObject asiakas = new PhysicsObject(5.0, 5.0, Shape.Circle);
        asiakas.Color = Color.Red;
        asiakas.Position = new Vector(0.0, 0.0);
        Add(asiakas,1);
        asiakas.Oscillate(Vector.UnitY, 50.0, 0.4);
        asiakas.CollisionIgnoreGroup = 1;  // Asiakas ei voi törmätä tahroihin.

        // kone.CollisionIgnoreGroup = 1;  // Kone (pois lukien harjaosa) ei vaikuta likaan mitenkään.
        // kokoKone.Add(kone);

        /*PhysicsObject koneenHarjaosa = new PhysicsObject(10, 40, Shape.Rectangle);
        koneenHarjaosa.Position = new Vector(0.0, 50.0);
        koneenHarjaosa.CollisionIgnoreGroup = 2;
        Add(koneenHarjaosa);
        */    

        //PhysicsStructure kokoKone = new PhysicsStructure(kone, koneenHarjaosa);
        //kokoKone.Softness = 0.0;

        Keyboard.Listen(Key.Up, ButtonState.Down, LiikutaEteen, "Liikuta konetta eteenpäin", kone, 3000.0);
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaTaakse, "Liikuta konetta taaksepäin", kone, 560.0);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanna, "Käännä konetta vasemmalle", kone, 4000.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanna, "Käännä konetta oikealle", kone, -4000.0);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohje");

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        AddCollisionHandler(kone, "lika", Puhdista);

        TileMap ruudut = TileMap.FromLevelAsset("kentta1");
        ruudut.SetTileMethod('=', TeePalikka);
        ruudut.SetTileMethod('#', TeeTahra);
        ruudut.Execute(10, 10);

        //Window.Height = 600;
        //Window.Width = 800;

        Camera.Follow(kone);  // Kamera kulkee koneen mukana.
        Camera.StayInLevel = true;  // Kamera ei mene kentän reunojen ulkopuolelle.

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

    public void TeePalikka(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject palikka = PhysicsObject.CreateStaticObject(leveys, korkeus);
        palikka.Position = paikka;
        palikka.Shape = Shape.Rectangle;
        palikka.Color = Color.Gray;
        Add(palikka);
    }

    public void TeeTahra(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahra = new PhysicsObject(leveys, korkeus);
        tahra.Position = paikka;
        tahra.Shape = Shape.Rectangle;
        tahra.Color = Color.Brown;
        tahra.CollisionIgnoreGroup = 1; // Tahrat eivät voi törmätä toisiinsa.
        tahra.Tag = "lika";
        Add(tahra);
    }

    public void Puhdista(PhysicsObject puhdistaja, PhysicsObject puhdistettava)
    {
        puhdistettava.Destroy();
    }

  
}
