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
    const double RUUDUN_SIVU = 10.0;  // Yhden (neliönmuotoisen) ruudun sivun pituus.
    const double AIKARAJOITUS = 100.0;  // Kentän läpäisemiseksi tarkoitettu aika.
    const int VAROITUSTEN_MAKSIMI = 3;  // Suurin mahdollinen pelaajan saamien varoitusten määrä.

    IntMeter tahralaskuri;
    IntMeter varoituslaskuri;

    public override void Begin()
    {
        Window.Height = 600;
        Window.Width = 800;

        TeeAlkuvalikko();
    }


    public void TeeAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pelin alkuvalikko", "Aloita peli", "Ohjeet", "Lopeta");
        alkuvalikko.AddItemHandler(0, AloitaPeli, TeeKone());
        alkuvalikko.AddItemHandler(1, NaytaOhjeet);
        alkuvalikko.AddItemHandler(2, Exit);
        Add(alkuvalikko);
    }


    public void AloitaPeli(PhysicsObject kone)
    {
        int kentanNro = 1;
        int varoitukset = 0;  // Pelaajan saamat varoitukset.
        SeuraavaKentta(kone, kentanNro, varoitukset);
        AddCollisionHandler(kone, "lika", Puhdista);
        AddCollisionHandler(kone, "asiakas", TormasitAsiakkaaseen);
    }


    /// <summary>
    /// Kentästä toiseen siirtyminen: nollataan kaikki, keskeytetään pause-tila ja siirrytään seuraavan kentän luomiseen.
    /// </summary>
    /// <param name="kone">Pelaaja.</param>
    /// <param name="kentanNro">Aloitettavan kentän numero.</param>
    /// <param name="varoitukset">Pelaajan saamien varoitusten määrä.</param>
    public void SeuraavaKentta(PhysicsObject kone, int kentanNro, int varoitukset)
    {
        ClearAll();
        IsPaused = false;
        LuoKentta(kone, kentanNro, AIKARAJOITUS, varoitukset);
    }


    /// <summary>
    /// Luodaan pelin ohjaimet.
    /// </summary>
    /// <param name="kone">Pelaajahahmo.</param>
    public void LuoOhjaimet(PhysicsObject kone)
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, Liikuta, "Liikuta konetta eteenpäin", kone, 3000.0);
        Keyboard.Listen(Key.Down, ButtonState.Down, Liikuta, "Liikuta konetta taaksepäin", kone, -560.0);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanna, "Käännä konetta vasemmalle", kone, 4000.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanna, "Käännä konetta oikealle", kone, -4000.0);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohje");
        Keyboard.Listen(Key.P, ButtonState.Pressed, Pause, "Keskeytä peli");
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    public void LuoKentta(PhysicsObject kone, int kentanNro, double aika, int varoitukset)
    {
        Level.CreateBorders();
        TeeAikalaskuri(aika);
        TeeVaroituslaskuri();
        varoituslaskuri.Value = varoitukset;
        TeeTahralaskuri(kone, kentanNro);
        LuoOhjaimet(kone);
        NaytaViikonpaiva(KerroViikonpaiva(kentanNro));

        TileMap ruudut = TileMap.FromLevelAsset("kentta" + kentanNro);
        ruudut.SetTileMethod('=', TeeSeina);
        ruudut.SetTileMethod('#', TeeTahra);
        ruudut.SetTileMethod('p', TeePoyta);
        ruudut.SetTileMethod('1', TeeAsiakas, 50.0);
        ruudut.SetTileMethod('2', TeeAsiakas, 100.0);
        ruudut.SetTileMethod('3', TeeAsiakas, 150.0);
        ruudut.SetTileMethod('4', TeeAsiakas, 200.0);
        ruudut.Execute(RUUDUN_SIVU, RUUDUN_SIVU);

        kone.Position = new Vector(Level.Left + 6 * RUUDUN_SIVU, 0);
        Add(kone);

        Camera.Follow(kone);  // Kamera kulkee koneen mukana.
        Camera.StayInLevel = true;  // Kamera ei mene kentän reunojen ulkopuolelle.
    }


    public string KerroViikonpaiva(int kentanNro)
    {
        string[] tyopaivat = { "Maanantai", "Tiistai", "Keskiviikko", "Torstai", "Perjantai" };
        for (int i = 0; i < tyopaivat.Length; i++)
        {
            if (kentanNro == i + 1) return tyopaivat[i];
        }
        return "";
    }


    public void NaytaViikonpaiva(string viikonpaiva)
    {
        Label viikonpaivanaytto = new Label(viikonpaiva);
        viikonpaivanaytto.Position = new Vector(0, 10 * RUUDUN_SIVU);
        Add(viikonpaivanaytto);
    }


    public void LapaisitPelin()
    {
        IsPaused = true;
        MessageDisplay.Add("Olet selvinnyt raskaasta työviikosta. Viikonloppu kutsuu! Paina Enter.");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Exit, "Siirry viikonlopun viettoon.");
    }
    

    public void NaytaOhjeet()
    {
        Exit();
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="kone"></param>
    /// <param name="kentanNro"></param>
    public void LapaisitKentan(PhysicsObject kone, int kentanNro)
    {
        kentanNro++;

        if (kentanNro > 5)  // Jos kyseessä oli viimeinen kenttä, lopetetaan peli...
        {
            LapaisitPelin();
        }
        else  // ...muuten jatketaan seuraavaan kenttään.
        {
            IsPaused = true;
            MessageDisplay.Add("Olet selvinnyt yhdestä työpäivästä. Hienoa! Paina Enter.");
            Keyboard.Listen(Key.Enter, ButtonState.Pressed, SeuraavaKentta, "Siirry seuraavaan kenttään", kone, kentanNro, varoituslaskuri.Value);
        }
    }


    public void Liikuta(PhysicsObject liikutettava, double vauhti)
    {
        Vector kulkusuunta = Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kulkusuunta);
    }


    public void Kaanna(PhysicsObject kaannettava, double vaantovoima)
    {
        kaannettava.ApplyTorque(vaantovoima);
    }


    public PhysicsObject TeeKone()
    {
        PhysicsObject kone = new PhysicsObject(5 * RUUDUN_SIVU, 2 * RUUDUN_SIVU, Shape.Rectangle);
        //kone.Color = Color.Orange;
        kone.Image = LoadImage("kone");
        kone.LinearDamping = 0.7;  // Määritellään, kuinka nopeasti koneen vauhti hidastuu.
        kone.Restitution = 0.3;  // Määritellään koneen kimmoisuus.
        kone.MomentOfInertia = 100;  // Koneen hitausmomentti.
        kone.AngularDamping = 0.6;  // Määritellään, kuinka nopeasti koneen kulmanopeus pienenee.
        //kone.Tag = "kone";
        Add(kone);

        return kone;
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
        varoituslaskuri.Value++;
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


    public void TeeTahralaskuri(PhysicsObject kone, int kentanNro)
    {
        tahralaskuri = new IntMeter(0);
        tahralaskuri.MinValue = 0;
        tahralaskuri.LowerLimit += delegate { LapaisitKentan(kone, kentanNro); };
        int tahrat = tahralaskuri.Value;
    }


    public void TeeAikalaskuri(double aika)
    {
        DoubleMeter ajanvahentaja = new DoubleMeter(aika);       

        Timer aikalaskuri = new Timer();
        aikalaskuri.Interval = 0.1;
        aikalaskuri.Timeout += delegate { LaskeAlaspain(aikalaskuri, ajanvahentaja); };
        aikalaskuri.Start();

        Label aikanaytto = new Label();
        aikanaytto.TextColor = Color.White;
        aikanaytto.DecimalPlaces = 1;
        aikanaytto.BindTo(ajanvahentaja);
        Add(aikanaytto);
    }


    public void LaskeAlaspain(Timer aikalaskuri, DoubleMeter ajanvahentaja)
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
        varoituslaskuri.MaxValue = VAROITUSTEN_MAKSIMI;
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
        IsPaused = true;
        MessageDisplay.Add("Olet toheloinut niin paljon, että sinut irtisanotaan. Hyvästi! Paina Enter.");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Exit, "Lopeta peli");
    }

}