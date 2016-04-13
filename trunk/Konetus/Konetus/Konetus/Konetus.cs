using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @author Marko Moilanen
/// @version 13.4.2016
/// <summary>
/// Konetus on ylhäältä kuvattu peli, jossa kuljetetaan lattioiden siivouksessa käytettävää yhdistelmäkonetta.
/// Tarkoitus on puhdistaa kaikki tahrat annetussa ajassa sekä välttyä törmäämästä alueella liikkuviin 
/// asiakkaisiin.
/// </summary>
public class Konetus : PhysicsGame
{
    // Koneeseen eli pelaajahahmoon liittyvät vakiot:
    private const double KONEEN_VAUHTI_ETEEN = 3000.0;
    private const double KONEEN_VAUHTI_TAAKSE = -560.0;
    private const double KONEEN_VAANTOVOIMA = 4000.0;
    private const double KONEEN_HIDASTUVUUS = 0.7; 
    private const double KONEEN_KIMMOISUUS = 0.3;
    private const double KONEEN_HITAUSMOMENTTI = 100;
    private const double KONEEN_KULMANOPEUDEN_HIDASTUVUUS = 0.6;

    // Pelissä on viisi kenttää:
    private const int KENTTIEN_MAARA = 5;

    // Pelialueella harhaileviin asiakkaisiin liittyvät vakiot:
    private const double ASIAKKAAN_KULKUSADE = 5 * RUUDUN_SIVU;
    private const double ASIAKKAAN_PERUSVAUHTI = 50.0;
    private const double RUUDUN_SIVU = 10.0;  // Yhden (neliönmuotoisen) ruudun sivun pituus.

    // Peliin liittyvät rajoituket:
    private const double AIKARAJOITUS = 100.0;  
    private const int VAROITUSTEN_MAKSIMI = 3;  
    
    // Jos asikakkaaseen törmää, on tuloksena räjähdys. Räjähdykseen liittyvät vakiot:
    private const double RAJAHDYKSEN_SADE = 60.0;
    private const double RAJAHDYKSEN_NOPEUS = 100.0;
    private const double RAJAHDYKSEN_VOIMA = 1000.0;

    // Ruudun koko:
    private const int IKKUNAN_KORKEUS = 600;
    private const int IKKUNAN_LEVEYS = 800;

    private IntMeter tahralaskuri;
    private IntMeter varoituslaskuri;


    /// <summary>
    /// Määritellään peli-ikkunan koko ja luodaan alkuvalikko.
    /// </summary>
    public override void Begin()
    {
        Window.Height = IKKUNAN_KORKEUS;
        Window.Width = IKKUNAN_LEVEYS;

        TeeAlkuvalikko();
    }


    /// <summary>
    /// Tekee pelin alkuvalikon.
    /// </summary>
    public void TeeAlkuvalikko()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Konetus", "Aloita peli", "Lopeta");
        alkuvalikko.AddItemHandler(0, AloitaPeli, TeeKone());
        alkuvalikko.AddItemHandler(1, Exit);
        Add(alkuvalikko);
    }


    /// <summary>
    /// Pelin aloitus: aloitetaan kenttien pelaaminen ja lisätään törmäyskäsittelijät.
    /// </summary>
    /// <param name="kone">Pelaaja.</param>
    public void AloitaPeli(PhysicsObject kone)
    {
        int kentanNro = 1;
        int varoitukset = 0;  // Pelaajan saamat varoitukset.
        SeuraavaKentta(kone, kentanNro, varoitukset);  // Siirrytään ensimmäiseen kenttään.
        AddCollisionHandler(kone, "lika", Puhdista);  // Kun pelaaja törmää tahraan, tahra puhdistuu.
        AddCollisionHandler(kone, "asiakas", TormasitAsiakkaaseen);  // Kun pelaaja törmää asiakkaaseen, tulee räjähdys ja asiakan tuhoutuu.
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
    /// Luo uuden kentän.
    /// </summary>
    /// <param name="kone">Pelaaja.</param>
    /// <param name="kentanNro">Luotavan kentän numero.</param>
    /// <param name="aika">Kentän läpäisemiseksi annettu aika.</param>
    /// <param name="varoitukset">Pelaajan tähän mennessä saamat varoitukset.</param>
    public void LuoKentta(PhysicsObject kone, int kentanNro, double aika, int varoitukset)
    {
        Level.CreateBorders();  // Kentän rajat.
        TeeAikalaskuri(aika);  // Aikalaskuri, joka laskee jäljellä olevan ajan.
        TeeVaroituslaskuri();  // Pidetään kirjaa pelaajan saamista varoituksista...
        varoituslaskuri.Value = varoitukset;  // ...mutta koska laskurit nollaantuvat SeuraavaKentta-aliohjelmassa, on 
                                              // laskurin arvoksi määriteltävä parametrina saatu varoitusten määrä.
                                              // Muutenhan edellisissä kentissä saadut varoitukset eivät enää olisi voimassa.
        TeeTahralaskuri(kone, kentanNro);  // Tahralaskuri, joka laskee, kuinka paljon puhdistettavaa on vielä jäljellä. 
                                           // Kun kaikki tahrat on puhdistettu, siirrytään seuraavaan kenttään.
        LuoOhjaimet(kone);
        NaytaViikonpaiva(KerroViikonpaiva(kentanNro));  // Näyttö, joka kertoo kentän numeroa vastaavan viikonpäivän.

        // Tehdään annettua kentän numeroa vastaava ruutukenttä:
        TileMap ruudut = TileMap.FromLevelAsset("kentta" + kentanNro);

        // Pelialueella on pöytiä ja seiniä. Ne ovat kiinteitä objekteja, jotka
        // rajoittavat pelaajan kulkua.
        ruudut.SetTileMethod('=', TeeSeina);
        ruudut.SetTileMethod('p', TeePoyta);

        // Pelialueella (lattialla) on tahroja, jotka täytyy siivota:
        ruudut.SetTileMethod('#', TeeTahra);

        // Pelialueella liikkuvia asiakkaita on neljää eri tyyppiä. Tyyppien erona on niiden vauhti.
        ruudut.SetTileMethod('1', TeeAsiakas, ASIAKKAAN_PERUSVAUHTI);
        ruudut.SetTileMethod('2', TeeAsiakas, 2 * ASIAKKAAN_PERUSVAUHTI);
        ruudut.SetTileMethod('3', TeeAsiakas, 3 * ASIAKKAAN_PERUSVAUHTI);
        ruudut.SetTileMethod('4', TeeAsiakas, 4 * ASIAKKAAN_PERUSVAUHTI);
        ruudut.Execute(RUUDUN_SIVU, RUUDUN_SIVU);

        // Määritellään pelaajan paikka ja lisätään pelaaja kentälle.
        kone.Position = new Vector(Level.Left + 6 * RUUDUN_SIVU, 0);
        Add(kone);

        Camera.Follow(kone);  // Kamera kulkee koneen mukana.
        Camera.StayInLevel = true;  // Kamera ei mene kentän reunojen ulkopuolelle.
    }


    /// <summary>
    /// Luo pelin ohjaimet.
    /// </summary>
    /// <param name="kone">Pelaajahahmo.</param>
    public void LuoOhjaimet(PhysicsObject kone)
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, Liikuta, "Liikuta konetta eteenpäin", kone, KONEEN_VAUHTI_ETEEN);
        Keyboard.Listen(Key.Down, ButtonState.Down, Liikuta, "Liikuta konetta taaksepäin", kone, KONEEN_VAUHTI_TAAKSE);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanna, "Käännä konetta vasemmalle", kone, KONEEN_VAANTOVOIMA);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanna, "Käännä konetta oikealle", kone, -KONEEN_VAANTOVOIMA);

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohje");
        Keyboard.Listen(Key.P, ButtonState.Pressed, Pause, "Keskeytä peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Palauttaa kentän numeroa vastaavan viikonpäivän.
    /// </summary>
    /// <param name="kentanNro"></param>
    /// <returns>Numeroa vastaavan viikkonpäivän nimi. 1 = maanantai, 2 = tiistai, ... .</returns>
    public static string KerroViikonpaiva(int kentanNro)
    {
        string[] tyopaivat = { "Maanantai", "Tiistai", "Keskiviikko", "Torstai", "Perjantai" };
        for (int i = 0; i < tyopaivat.Length; i++)
        {
            if (kentanNro == i + 1) return tyopaivat[i];
        }
        return "";
    }


    /// <summary>
    /// Lisää pelialueelle näytön, joka kertoo menossa olevan viikonpäivän.
    /// </summary>
    /// <param name="viikonpaiva"></param>
    public void NaytaViikonpaiva(string viikonpaiva)
    {
        Label viikonpaivanaytto = new Label(viikonpaiva);
        viikonpaivanaytto.Position = new Vector(0, 10 * RUUDUN_SIVU);
        Add(viikonpaivanaytto);
    }
    
    
    /// <summary>
    /// Siirrytään seuraavaan kenttään tai, jos kaikki kentät on läpäisty, lopetetaan peli.
    /// </summary>
    /// <param name="kone">Pelaaja.</param>
    /// <param name="kentanNro">Meneillään olevan kentän numero.</param>
    public void LapaisitKentan(PhysicsObject kone, int kentanNro)
    {
        kentanNro++;

        if (kentanNro > KENTTIEN_MAARA)  // Jos kyseessä oli viimeinen kenttä, lopetetaan peli...
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


    /// <summary>
    /// Liikuttaa fysiikkoliota (translaatio).
    /// </summary>
    /// <param name="liikutettava">Liikutettava fysiikkaolio.</param>
    /// <param name="vauhti">Olion vauhti.</param>
    public void Liikuta(PhysicsObject liikutettava, double vauhti)
    {
        Vector kulkusuunta = Vector.FromLengthAndAngle(vauhti, liikutettava.Angle);
        liikutettava.Push(kulkusuunta);
    }


    /// <summary>
    /// Kääntää fysiikkaoliota (rotaatio).
    /// </summary>
    /// <param name="kaannettava">Käännettävä fysiikkaolio.</param>
    /// <param name="vaantovoima">Vääntövoima. Jos vääntövoima on positiivinen, käännetään vastapäivään, muuten myötäpäivään.</param>
    public void Kaanna(PhysicsObject kaannettava, double vaantovoima)
    {
        kaannettava.ApplyTorque(vaantovoima);
    }


    /// <summary>
    /// Tekee fysiikkaolion, joka on samalla pelaajahahmo.
    /// </summary>
    /// <returns>Pelaajahahmo.</returns>
    public PhysicsObject TeeKone()
    {
        PhysicsObject kone = new PhysicsObject(5 * RUUDUN_SIVU, 2 * RUUDUN_SIVU, Shape.Rectangle);
        kone.Image = LoadImage("kone");
        kone.LinearDamping = KONEEN_HIDASTUVUUS;
        kone.Restitution = KONEEN_KIMMOISUUS;
        kone.MomentOfInertia = KONEEN_HITAUSMOMENTTI;
        kone.AngularDamping = KONEEN_KULMANOPEUDEN_HIDASTUVUUS;
        Add(kone);

        return kone;
    }


    /// <summary>
    /// Tekee staattisen suorakulmion muotoisen fysiikkaolion.
    /// </summary>
    /// <param name="paikka">Keskipisteen sijainti..</param>
    /// <param name="leveys">Palikan leveys.</param>
    /// <param name="korkeus">Palikan korkeus.</param>
    /// <param name="vari">Palikan väri.</param>
    public void TeePalikka(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject palikka = PhysicsObject.CreateStaticObject(leveys, korkeus);
        palikka.Position = paikka;
        palikka.Color = vari;
        Add(palikka);
    }


    /// <summary>
    /// Tekee seinäpalikan.
    /// </summary>
    /// <param name="paikka">Palikan keskipisteen sijainti.</param>
    /// <param name="leveys">Palikan leveys.</param>
    /// <param name="korkeus">Palikan korkeus.</param>
    public void TeeSeina(Vector paikka, double leveys, double korkeus)
    {
        TeePalikka(paikka, leveys, korkeus, Color.Gray);
    }


    /// <summary>
    /// Tekee seinäpalikan.
    /// </summary>
    /// <param name="paikka">Palikan keskipisteen sijainti.</param>
    /// <param name="leveys">Palikan leveys.</param>
    /// <param name="korkeus">Palikan korkeus.</param>
    public void TeePoyta(Vector paikka, double leveys, double korkeus)
    {
        TeePalikka(paikka, leveys, korkeus, Color.Brown);
    }


    /// <summary>
    /// Tekee puhdistettavan (kerättävän) fysiikkaolion. 
    /// </summary>
    /// <param name="paikka">Keskipisteen sijainti.</param>
    /// <param name="leveys">Tahran leveys.</param>
    /// <param name="korkeus">Tahran korkeus.</param>
    public void TeeTahra(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahra = new PhysicsObject(leveys, korkeus);
        tahra.Position = paikka;
        tahra.Color = new Color(170, 220, 100, 255);  // Tahran väri. Arvot etsitty kokeilemalla.
        tahra.CollisionIgnoreGroup = 1; // Tahrat eivät voi törmätä toisiinsa.
        tahra.IgnoresExplosions = true;  // Räjähdykset eivät vaikuta tahroihin.
        tahra.Tag = "lika";
        tahralaskuri.Value += 1;  // Tahrojen määrästä on pidettävä kirjaa.
        Add(tahra);
    }


    /// <summary>
    /// Puhdistaa (tuhoaa) fysiikkaolion.
    /// </summary>
    /// <param name="puhdistaja">Fysiikkaolio, joka suorittaa puhdistamisen.</param>
    /// <param name="puhdistettava">Puhdistettava fysiikkaolio.</param>
    public void Puhdista(PhysicsObject puhdistaja, PhysicsObject puhdistettava)
    {
        puhdistettava.Destroy();
        tahralaskuri.Value -= 1;  // Puhdistettavien olioiden määrästä on pidettävä kirjaa.
    }


    /// <summary>
    /// Kun kaksi fysiikkoliota törmää, tulee räjähdys ja törmäyksen kohde tuhoutuu.
    /// </summary>
    /// <param name="tormaaja">Fysiikkolio, joka törmää.</param>
    /// <param name="kohde">Törmäyksen kohde.</param>
    public void TormasitAsiakkaaseen(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        Explosion rajahdys = new Explosion(RAJAHDYKSEN_SADE);
        rajahdys.Position = kohde.Position;
        rajahdys.Speed = RAJAHDYKSEN_NOPEUS;
        rajahdys.Force = RAJAHDYKSEN_VOIMA;
        Add(rajahdys);
        kohde.Destroy();
        varoituslaskuri.Value++;  // Törmäys aiheuttaa törmääjälle varoituksen.
        MessageDisplay.Add("Tunari! Törmäsit koneella asiakkaaseen!");
    }


    /// <summary>
    /// Luo satunnaisesti liikkuvan fysiikkaolion.
    /// </summary>
    /// <param name="paikka">Sijainti.</param>
    /// <param name="leveys">Leveys.</param>
    /// <param name="korkeus">Korkeus.</param>
    /// <param name="vauhti">Liikkumisvauhti.</param>
    public void TeeAsiakas(Vector paikka, double leveys, double korkeus, double vauhti)
    {
        PhysicsObject asiakas = new PhysicsObject(leveys, korkeus, Shape.Circle);
        asiakas.Position = paikka;
        asiakas.Color = RandomGen.NextColor();  // Valitaan satunnainen väri.
        asiakas.CollisionIgnoreGroup = 1;  // Asiakas ei voi törmätä tahroihin.
        asiakas.Tag = "asiakas";
        Add(asiakas, 1);

        // Luodaan asiakkaalle tekoäly:
        RandomMoverBrain asiakkaanAivot = new RandomMoverBrain(vauhti);
        asiakkaanAivot.ChangeMovementSeconds = 3;
        asiakkaanAivot.WanderRadius = ASIAKKAAN_KULKUSADE;
        asiakas.Brain = asiakkaanAivot;
    }


    /// <summary>
    /// Tekee laskurin, joka pitää kirjaa tuhottavista fysiikkaolioista (tahroista).
    /// </summary>
    /// <param name="kone">Pelaaja.</param>
    /// <param name="kentanNro">Meneillä olevan kentän numero.</param>
    public void TeeTahralaskuri(PhysicsObject kone, int kentanNro)
    {
        tahralaskuri = new IntMeter(0);
        tahralaskuri.MinValue = 0;
        tahralaskuri.LowerLimit += delegate { LapaisitKentan(kone, kentanNro); };  // Kun kaikki tahrat on puhdistettu, kenttä on läpäisty.
    }


    /// <summary>
    /// Tekee ajastimen, joka mittaa jäljellä olevaa aikaa, ja aikanäytön, joka näyttää jäljellä olevan ajan.
    /// </summary>
    /// <param name="aika">Aika, josta laskenta aloitetaan (sekunteina).</param>
    public void TeeAikalaskuri(double aika)
    {
        // Desimaalilukulaskuri, joka aloittaa laskemisen käytettävissä olevasta ajasta.
        DoubleMeter aikalaskuri = new DoubleMeter(aika);

        // Timer-tyyppisen ajastimen tehtävänä on kutsua 0.1 sekunnin välein LaskeAlaspain-aliohjelmaa, 
        // joka vähentää ajanvahentajan arvoa 0.1 sekunnin verran. Timer-ajastin ei siis
        // ole tässä pelissä varsinainen ajastin, joka laskee jäljellä olevan ajan.
        Timer ajastin = new Timer();
        ajastin.Interval = 0.1;
        ajastin.Timeout += delegate { LaskeAlaspain(ajastin, aikalaskuri); };
        ajastin.Start();

        // Tehdään laskurille näyttö:
        Label aikanaytto = new Label();
        aikanaytto.TextColor = Color.White;
        aikanaytto.DecimalPlaces = 1;
        aikanaytto.BindTo(aikalaskuri);
        Add(aikanaytto);
    }


    /// <summary>
    /// Laskee aikaa alaspäin.
    /// </summary>
    /// <param name="ajastin">Ajastin, joka määrittää, kuinka usein tätä aliohjelmaa kutsutaan.</param>
    /// <param name="aikalaskuri">Mittari, joka mittaa jäljellä o</param>
    public void LaskeAlaspain(Timer ajastin, DoubleMeter aikalaskuri)
    {
        aikalaskuri.Value -= 0.1;

        // Määritellään, mitä tapahtuu, kun aika on loppunut.
        if (aikalaskuri.Value <= 0)
        {
            MessageDisplay.Add("Aijai... Et pysynyt aikataulussa. Tästä hyvästä tulee varoitus.");
            ajastin.Stop();  // Pysäytetään ajastin, koska tätä aliohjelmaa ei enää tarvitse kutsua.
            varoituslaskuri.Value += 1;  // Kun aika loppuu, pelaajalle tulee varoitus.
        }
    }

    
    /// <summary>
    /// Tekee laskurin, joka laskee pelaajan saamien varoitusten määrän, ja laskurin näytön.
    /// </summary>
    public void TeeVaroituslaskuri()
    {
        // Itse laskuri
        varoituslaskuri = new IntMeter(0);
        varoituslaskuri.MaxValue = VAROITUSTEN_MAKSIMI;
        varoituslaskuri.UpperLimit += PotkutTuli;

        // Laskurin näyttö:
        Label varoitusnaytto = new Label();
        varoitusnaytto.Position = new Vector(0, Level.Top - 10 * RUUDUN_SIVU);
        varoitusnaytto.TextColor = Color.Black;
        varoitusnaytto.Color = Color.Red;
        varoitusnaytto.BindTo(varoituslaskuri);
        Add(varoitusnaytto);
    }


    /// <summary>
    /// Keskeyttää pelin ja ilmoittaa pelaajalle pelin loppumisesta.
    /// </summary>
    public void PotkutTuli()
    {
        IsPaused = true;  // Keskeytetään peli.
        MessageDisplay.Add("Olet toheloinut niin paljon, että sinut irtisanotaan. Hyvästi! Paina Enter.");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Exit, "Lopeta peli");
    }


    /// <summary>
    /// Keskeyttää pelin ja kertoo pelaajan läpäisseen kaikki kentät.
    /// </summary>
    public void LapaisitPelin()
    {
        IsPaused = true;
        MessageDisplay.Add("Olet selvinnyt raskaasta työviikosta. Viikonloppu kutsuu! Paina Enter.");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Exit, "Siirry viikonlopun viettoon.");
    }
}