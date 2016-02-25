using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class Konetus : PhysicsGame
{
    const double RUUDUN_SIVU = 10.0;  // Yhden (neliönmuotoisen) ruudun sivun pituus.
    int kenttaNro = 1;

    PhysicsObject kone = new PhysicsObject(4 * RUUDUN_SIVU, 2 * RUUDUN_SIVU, Shape.Rectangle);

    IntMeter tahralaskuri;  // Puhdistamattomien tahrojen määrä.

    public override void Begin()
    {
        //Level.CreateBorders();

        SeuraavaKentta();

        AddCollisionHandler(kone, "lika", Puhdista);
        AddCollisionHandler(kone, "asiakas", TormasitAsiakkaaseen);

        //Window.Height = 600;
        //Window.Width = 800;
    }


    public void LuoOhjaimet()
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, LiikutaEteen, "Liikuta konetta eteenpäin", kone, 3000.0);
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaTaakse, "Liikuta konetta taaksepäin", kone, 560.0);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanna, "Käännä konetta vasemmalle", kone, 4000.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanna, "Käännä konetta oikealle", kone, -4000.0);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohje");

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    public void SeuraavaKentta()
    {
        ClearAll();

        if (kenttaNro == 1)
        {
            LuoKentta("kentta1");
        }
        else LuoKentta("kentta2");
        if (kenttaNro > 2) Exit();
    }


    public void LuoKentta(string kenttatiedosto)
    {
        TeeTahralaskuri();
        LuoOhjaimet();
        TileMap ruudut = TileMap.FromLevelAsset(kenttatiedosto);
        ruudut.SetTileMethod('=', TeeSeina);
        ruudut.SetTileMethod('#', TeeTahra);
        ruudut.SetTileMethod('p', TeePoyta);
        ruudut.SetTileMethod('1', TeeAsiakas1);
        ruudut.SetTileMethod('2', TeeAsiakas2);
        ruudut.SetTileMethod('3', TeeAsiakas3);
        ruudut.SetTileMethod('4', TeeAsiakas4);
        ruudut.Execute(RUUDUN_SIVU, RUUDUN_SIVU);

        kone.Position = new Vector(Level.Left + kone.Width, 0);
        kone.Color = Color.Orange;
        kone.LinearDamping = 0.7;  // Määritellään, kuinka nopeasti koneen vauhti hidastuu.
        kone.Restitution = 0.3;  // Määritellään koneen kimmoisuus.
        kone.MomentOfInertia = 100;  // Koneen hitausmomentti.
        kone.AngularDamping = 0.6;  // Määritellään, kuinka nopeasti koneen kulmanopeus pienenee.
        Add(kone);

        Camera.Follow(kone);  // Kamera kulkee koneen mukana.
        Camera.StayInLevel = true;  // Kamera ei mene kentän reunojen ulkopuolelle.
    }

    public void LapaisitKentan()
    {
        kenttaNro++;
        SeuraavaKentta();
    }

    public void LiikutaEteen(PhysicsObject liikutettava, double vauhti)
    {
        Vector kulkusuunta = Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kulkusuunta);
    }


    public void LiikutaTaakse(PhysicsObject liikutettava, double vauhti)
    {
        Vector kaanteinenKulkusuunta = -Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kaanteinenKulkusuunta);
    }


    public void Kaanna(PhysicsObject kaannettava, double vaantovoima)
    {
        kaannettava.ApplyTorque(vaantovoima);
    }


    public void TeePalikka(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject palikka = PhysicsObject.CreateStaticObject(leveys, korkeus);
        palikka.Position = paikka;
        palikka.Color = vari;
        Add(palikka);
    }


    public void TeeSeina(Vector paikka, double leveys, double korkeus)
    {
        TeePalikka(paikka, leveys, korkeus, Color.Gray);
    }


    public void TeePoyta(Vector paikka, double leveys, double korkeus)
    {
        TeePalikka(paikka, leveys, korkeus, Color.Brown);
    }


    public void TeeTahra(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahra = new PhysicsObject(leveys, korkeus);
        tahra.Position = paikka;
        tahra.Shape = Shape.Rectangle;
        tahra.Color = Color.Charcoal;
        tahra.CollisionIgnoreGroup = 1; // Tahrat eivät voi törmätä toisiinsa.
        tahra.IgnoresExplosions = true;  // Räjähdykset eivät vaikuta tahroihin.
        tahra.Tag = "lika";
        tahralaskuri.Value += 1;
        Add(tahra);
    }


    public void Puhdista(PhysicsObject puhdistaja, PhysicsObject puhdistettava)
    {
        puhdistettava.Destroy();
        tahralaskuri.Value -= 1;
    }


    public void TormasitAsiakkaaseen(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        Explosion rajahdys = new Explosion(60);
        rajahdys.Position = kohde.Position;
        rajahdys.Speed = 100.0;
        rajahdys.Force = 1000.0;
        Add(rajahdys);
        kohde.Destroy();
    }


    /// <summary>
    /// Aliohjelma luo olion, joka värähtelee annetulla taajuudella.
    /// </summary>
    /// <param name="paikka">Olion paikka (värähtelyn keskipiste).</param>
    /// <param name="leveys">Olion leveys.</param>
    /// <param name="korkeus">Olion korkeus.</param>
    /// <param name="suunta">Värähtelyn suunta.</param>
    /// <param name="amplitudi">Värähtelyn amplitudi.</param>
    /// <param name="taajuusHz">Värähtelyn taajuus hertseinä.</param>
    /// <param name="vari">Olion väri.</param>
    public void TeeAsiakas(Vector paikka, double leveys, double korkeus, Vector suunta, double amplitudi, double taajuusHz, Color vari)
    {
        PhysicsObject asiakas = new PhysicsObject(leveys, korkeus, Shape.Circle);
        asiakas.Position = paikka;
        asiakas.Color = vari;
        asiakas.Oscillate(suunta, amplitudi, taajuusHz);
        asiakas.CollisionIgnoreGroup = 1;  // Asiakas ei voi törmätä tahroihin.
        asiakas.Tag = "asiakas";
        Add(asiakas, 1);
    }


    public void TeeAsiakas1(Vector paikka, double leveys, double korkeus)
    {
        TeeAsiakas(paikka, leveys, korkeus, Vector.UnitY, 5 * RUUDUN_SIVU, 0.3, Color.Beige);
    }


    public void TeeAsiakas2(Vector paikka, double leveys, double korkeus)
    {
        TeeAsiakas(paikka, leveys, korkeus, Vector.UnitX, 5 * RUUDUN_SIVU, 0.3, Color.Black);
    }


    public void TeeAsiakas3(Vector paikka, double leveys, double korkeus)
    {
        TeeAsiakas(paikka, leveys, korkeus, Vector.UnitY, 10 * RUUDUN_SIVU, 1.0, Color.HotPink);
    }


    public void TeeAsiakas4(Vector paikka, double leveys, double korkeus)
    {
        TeeAsiakas(paikka, leveys, korkeus, Vector.UnitX, 10 * RUUDUN_SIVU, 1.0, Color.BloodRed);
    }


    public void TeeTahralaskuri()
    {
        tahralaskuri = new IntMeter(0);
        tahralaskuri.MinValue = 0;
        tahralaskuri.LowerLimit += LapaisitKentan;
        int tahrat = tahralaskuri.Value;
    }
}