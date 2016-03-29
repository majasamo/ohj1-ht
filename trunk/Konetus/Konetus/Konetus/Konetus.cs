using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @author Marko Moilanen
/// @version 1.3.2016
/// <summary>
/// 
/// </summary>
public class Konetus : PhysicsGame
{
    const double ASIAKKAAN_KULKUSADE = 5 * RUUDUN_SIVU;
    const double AIKARAJOITUS = 100.0;  // Kentän läpäisemiseksi tarkoitettu aika.
    const double RUUDUN_SIVU = 10.0;  // Yhden (neliönmuotoisen) ruudun sivun pituus.
    int kenttaNro = 1;

    PhysicsObject kone = new PhysicsObject(5 * RUUDUN_SIVU, 2 * RUUDUN_SIVU, Shape.Rectangle);

    IntMeter tahralaskuri;  // Puhdistamattomien tahrojen määrä.
    DoubleMeter ajanvahentaja;
    Timer aikalaskuri;
    IntMeter varoituslaskuri;

    int varoitukset = 0;  // Pelaajan saamat varoitukset.

    public override void Begin()
    {
        Window.Height = 600;
        Window.Width = 800;

        TeeAlkuvalikko();

        AddCollisionHandler(kone, "lika", Puhdista);
        AddCollisionHandler(kone, "asiakas", TormasitAsiakkaaseen);
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


    public void SeuraavaKentta(int varoitustenMaara)
    {
        ClearAll();
        if (kenttaNro > 5) LapaisitPelin();
        LuoKentta("kentta" + kenttaNro, AIKARAJOITUS, varoitustenMaara);
    }


    public void LuoKentta(string kenttatiedosto, double aika, int varoitustenMaara)
    {
        Level.CreateBorders();
        TeeAikalaskuri(aika);
        TeeVaroituslaskuri();
        varoituslaskuri.Value = varoitustenMaara;
        TeeTahralaskuri();
        LuoOhjaimet();
        TileMap ruudut = TileMap.FromLevelAsset(kenttatiedosto);
        ruudut.SetTileMethod('=', TeeSeina);
        ruudut.SetTileMethod('#', TeeTahra);
        ruudut.SetTileMethod('p', TeePoyta);
        ruudut.SetTileMethod('1', TeeAsiakas, 50.0);
        ruudut.SetTileMethod('2', TeeAsiakas, 100.0);
        ruudut.SetTileMethod('3', TeeAsiakas, 150.0);
        ruudut.SetTileMethod('4', TeeAsiakas, 200.0);
        ruudut.Execute(RUUDUN_SIVU, RUUDUN_SIVU);

        TeeKone();


        Camera.Follow(kone);  // Kamera kulkee koneen mukana.
        Camera.StayInLevel = true;  // Kamera ei mene kentän reunojen ulkopuolelle.
    }


    public void LapaisitPelin()
    {
        Exit();
    }


    public void TeeAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pelin alkuvalikko", "Aloita peli", "Ohjeet", "Lopeta");
        alkuvalikko.AddItemHandler(0, AloitaPeli);
        alkuvalikko.AddItemHandler(1, NaytaOhjeet);
        alkuvalikko.AddItemHandler(2, Exit);
        Add(alkuvalikko);
    }


    public void AloitaPeli()
    {
        SeuraavaKentta(varoitukset);
    }


    public void NaytaOhjeet()
    {
        Exit();
    }


    public void LapaisitKentan()
    {
        kenttaNro++;
        varoitukset = varoituslaskuri.Value;
        SeuraavaKentta(varoitukset);
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


    public void TeeKone()
    {
        kone.Position = new Vector(Level.Left + 6 * RUUDUN_SIVU, 0);
        kone.Color = Color.Orange;
        kone.Image = LoadImage("kone");
        kone.LinearDamping = 0.7;  // Määritellään, kuinka nopeasti koneen vauhti hidastuu.
        kone.Restitution = 0.3;  // Määritellään koneen kimmoisuus.
        kone.MomentOfInertia = 100;  // Koneen hitausmomentti.
        kone.AngularDamping = 0.6;  // Määritellään, kuinka nopeasti koneen kulmanopeus pienenee.
        Add(kone);
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
        tahra.Color = new Color(170, 220, 100, 255);  // Tahran väri. Arvot etsitty kokeilemalla.
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
        varoituslaskuri.Value += 1;
        MessageDisplay.Add("Tunari! Törmäsit koneella asiakkaaseen!");
    }


    public void TeeAsiakas(Vector paikka, double leveys, double korkeus, double vauhti)
    {
        PhysicsObject asiakas = new PhysicsObject(leveys, korkeus, Shape.Circle);
        asiakas.Position = paikka;
        asiakas.Color = RandomGen.NextColor();  // Valitaan satunnainen väri.
        asiakas.CollisionIgnoreGroup = 1;  // Asiakas ei voi törmätä tahroihin.
        asiakas.Tag = "asiakas";
        Add(asiakas, 1);

        RandomMoverBrain asiakkaanAivot = new RandomMoverBrain(vauhti);
        asiakkaanAivot.ChangeMovementSeconds = 3;
        asiakkaanAivot.WanderRadius = ASIAKKAAN_KULKUSADE;
        asiakas.Brain = asiakkaanAivot;
    }


    public void TeeTahralaskuri()
    {
        tahralaskuri = new IntMeter(0);
        tahralaskuri.MinValue = 0;
        tahralaskuri.LowerLimit += LapaisitKentan;
        int tahrat = tahralaskuri.Value;
    }


    public void TeeAikalaskuri(double aika)
    {
        ajanvahentaja = new DoubleMeter(aika);       

        aikalaskuri = new Timer();
        aikalaskuri.Interval = 0.1;
        aikalaskuri.Timeout += LaskeAlaspain;
        aikalaskuri.Start();

        Label aikanaytto = new Label();
        aikanaytto.TextColor = Color.White;
        aikanaytto.DecimalPlaces = 1;
        aikanaytto.BindTo(ajanvahentaja);
        Add(aikanaytto);
    }


    public void LaskeAlaspain()
    {
        ajanvahentaja.Value -= 0.1;

        if (ajanvahentaja.Value <= 0)
        {
            MessageDisplay.Add("Aijai... Et pysynyt aikataulussa. Tästä hyvästä tulee varoitus.");
            aikalaskuri.Stop();
            varoituslaskuri.Value += 1;
        }
    }

    
    public void TeeVaroituslaskuri()
    {
        varoituslaskuri = new IntMeter(0);
        varoituslaskuri.MaxValue = 3;
        varoituslaskuri.UpperLimit += PotkutTuli;

        Label varoitusnaytto = new Label();
        varoitusnaytto.Position = new Vector(0, Level.Top - 10 * RUUDUN_SIVU);
        varoitusnaytto.TextColor = Color.Black;
        varoitusnaytto.Color = Color.Red;
        varoitusnaytto.BindTo(varoituslaskuri);
        Add(varoitusnaytto);
    }


    public void PotkutTuli()
    {
        MessageDisplay.Add("Olet toheloinut niin paljon, että sinut irtisanotaan. Olet heikoin lenkki. Hyvästi! Paina Enter.");  // Tämä ei toimi vielä, vaan peli pelkästään loppuu.
        Exit();
    }

}