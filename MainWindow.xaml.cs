﻿using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las
        public const int LAKA = 2;     // łąka
        public const int SKALA = 3;   // skały
        public const int WROG = 4;   //przeciwnik
        public const int ILE_TERENOW = 5;   // ile terenów
        // Mapa przechowywana jako tablica dwuwymiarowa int
        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        // Dwuwymiarowa tablica kontrolek Image reprezentujących segmenty mapy
        private Image[,] tablicaTerenu;
        // Rozmiar jednego segmentu mapy w pikselach
        private const int RozmiarSegmentu = 32;

        // Tablica obrazków terenu – indeks odpowiada rodzajowi terenu
        // Indeks 1: las, 2: łąka, 3: skały
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        // Pozycja gracza na mapie
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        // Obrazek reprezentujący gracza
        private Image obrazGracza;
        // Licznik zgromadzonego drewna
        private int iloscDrewna = 0;
        private int punktyZycia = 3;
        private Image obrazPrzeciwnika;
        //private int przeciwnikX = 2;
        //private int przeciwnikY = 2;
        private int celDrewna = 10;


        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            // Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }
        private void WczytajObrazyTerenu()
        {
            // Zakładamy, że tablica jest indeksowana od 0, ale używamy indeksów 1-3
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[WROG] = new BitmapImage(new Uri("przeciwnik.png", UriKind.Relative));
        }

        // Wczytuje mapę z pliku tekstowego i dynamicznie tworzy tablicę kontrolek Image
        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                // Sprawdzamy, czy plik istnieje
                if (!File.Exists(sciezkaPliku))
                {
                    EtykietaKomunikat.Content =("Plik mapy nie istnieje: " + sciezkaPliku);
                    return;
                }
                var linie = File.ReadAllLines(sciezkaPliku);//zwraca tablicę stringów, np. linie[0] to pierwsza linia pliku
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;//zwraca liczbę elementów w tablicy
                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);//zwraca tablicę stringów np. czesci[0] to pierwszy element linii
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(czesci[x]);//wczytanie mapy z pliku
                    }
                }

                // Przygotowanie kontenera SiatkaMapy – czyszczenie elementów i definicji wierszy/kolumn
                SiatkaMapy.Children.Clear();
                SiatkaMapy.RowDefinitions.Clear();
                SiatkaMapy.ColumnDefinitions.Clear();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                }
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                }

                // Tworzenie tablicy kontrolk Image i dodawanie ich do siatki
                tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        Image obraz = new Image
                        {
                            Width = RozmiarSegmentu,
                            Height = RozmiarSegmentu
                        };

                        int rodzaj = mapa[y, x];
                        if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
                        {
                            obraz.Source = obrazyTerenu[rodzaj];//wczytanie obrazka terenu
                        }
                        else
                        {
                            obraz.Source = null;
                        }

                        Grid.SetRow(obraz, y);
                        Grid.SetColumn(obraz, x);
                        SiatkaMapy.Children.Add(obraz);//dodanie obrazka do siatki na ekranie
                        tablicaTerenu[y, x] = obraz;
                    }
                }

                // Dodanie obrazka gracza – ustawiamy go na wierzchu
                SiatkaMapy.Children.Add(obrazGracza);
                Panel.SetZIndex(obrazGracza, 1);//ustawienie obrazka gracza na wierzchu
                pozycjaGraczaX = 0;
                pozycjaGraczaY = 0;
                AktualizujPozycjeGracza();

                iloscDrewna = 0;
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                //DodajPrzeciwnika();
            }//koniec try
            catch (Exception ex)
            {
                EtykietaKomunikat.Content = "Błąd wczytywania mapy: " + ex.Message;

            }
            PasekDrewna.Value = 0;
            PasekDrewna.Maximum = celDrewna;

            PasekHP.Value = punktyZycia;

        }

        // Aktualizuje pozycję obrazka gracza w siatce
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        // Obsługa naciśnięć klawiszy – ruch gracza oraz wycinanie lasu
        private async void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up || e.Key == Key.W) nowyY--;
            else if (e.Key == Key.Down || e.Key == Key.S) nowyY++;
            else if (e.Key == Key.Left || e.Key == Key.A) nowyX--;
            else if (e.Key == Key.Right || e.Key == Key.D) nowyX++;

            // Ruch gracza w granicach mapy
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                int typPola = mapa[nowyY, nowyX];

                if (typPola != SKALA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }

                if (typPola == WROG)
                {
                    punktyZycia--;
                    PasekHP.Value = punktyZycia;
                    EtykietaHP.Content = "Życie: " + punktyZycia;
                    await DotkientyPrzezPrzeciwnika();

                    if (punktyZycia <= 0)
                    {
                       await WyswietlKomunikatPrzegranej();
                        ResetujGre();
                    }
                }
            }

            // Obsługa wycinania lasu
            if (e.Key == Key.C)
            {
                if (e.Key == Key.C)
                {
                    if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
                    {
                        mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                        tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                        iloscDrewna++;
                        EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                        PasekDrewna.Value = iloscDrewna;

                        if (iloscDrewna >= celDrewna)
                        {
                            await WyswietlKomunikatWygranej();
                            ResetujGre();
                        }
                    }
                }

            }
        }

        // Obsługa przycisku "Wczytaj mapę"
        //private void WczytajMape_Click(object sender, RoutedEventArgs e)
        //{
        //   OpenFileDialog oknoDialogowe = new OpenFileDialog();
        //   oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
        //    oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; // Ustawienie katalogu początkowego
        //   bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
        //    if (czyOtwartoMape == true)
        //    {
        //       WczytajMape(oknoDialogowe.FileName);
        //    }
        //}
        //private void DodajPrzeciwnika()
        //{
        //    obrazPrzeciwnika = new Image
        //    {
        //        Width = RozmiarSegmentu,
        //        Height = RozmiarSegmentu
        //    };
        //    BitmapImage bmpPrzeciwnika = new BitmapImage(new Uri("przeciwnik.png", UriKind.Relative));
        //    obrazPrzeciwnika.Source = bmpPrzeciwnika;

        //    SiatkaMapy.Children.Add(obrazPrzeciwnika);
        //    Panel.SetZIndex(obrazPrzeciwnika, 1);
        //    AktualizujPozycjePrzeciwnika();
        //}

        //private void AktualizujPozycjePrzeciwnika()
        //{
        //    Grid.SetRow(obrazPrzeciwnika, przeciwnikY);
        //    Grid.SetColumn(obrazPrzeciwnika, przeciwnikX);
        //}
        private void Mapa1_Click(object sender, RoutedEventArgs e)
        {
            celDrewna = 10;
            EtykietaWymaganegoDrewna.Content = "Wymagan liczba drewna by wygrać: " + celDrewna;
            WczytajMape("mapa1.txt");//mapa 5x5
        }

        private void Mapa2_Click(object sender, RoutedEventArgs e)
        {
            celDrewna = 15;
            EtykietaWymaganegoDrewna.Content = "Wymagan liczba drewna by wygrać: " + celDrewna;
            WczytajMape("mapa2.txt");//mapa 8x8
        }

        private void Mapa3_Click(object sender, RoutedEventArgs e)
        {
            celDrewna = 20;
            EtykietaWymaganegoDrewna.Content = "Wymagan liczba drewna by wygrać: " + celDrewna;
            WczytajMape("mapa3.txt");//mapa 10x10
        }

        private void wybierz_Click(object sender, RoutedEventArgs e)
        {
            var menu = (sender as Button).ContextMenu;
            menu.IsOpen = true;
        }
        private void RestartujGre_Click(object sender, RoutedEventArgs e)
        {
            ResetujGre(); // Uruchamia funkcję resetującą grę
        }
        private void ResetujGre()
        {
            
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            punktyZycia = 3;
            iloscDrewna = 0;

            
            EtykietaHP.Content = "HP: " + punktyZycia;
            EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
            EtykietaKomunikat.Content = "";

            
            SiatkaMapy.Children.Clear();

            
            EtykietaKomunikat.Content = "Nowa gra rozpoczęta! Wybierz mapę! Powodzenia!";
        }
        private async Task WyswietlKomunikatWygranej()
        {
            
            EtykietaKomunikat.Content = "Wygrałeś! Gratulacje!";

            
            await Task.Delay(3000); 

           
            EtykietaKomunikat.Content = ""; 
        }
        private async Task WyswietlKomunikatPrzegranej()
        {

            EtykietaKomunikat.Content = "Przegrałeś!";


            await Task.Delay(3000);


            EtykietaKomunikat.Content = "";
        }
        private async Task DotkientyPrzezPrzeciwnika()
        {

            EtykietaKomunikat.Content = "Zaatakował cię wróg!";


            await Task.Delay(1000);


            EtykietaKomunikat.Content = "";
        }



    }

}


