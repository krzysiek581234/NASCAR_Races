using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace NASCAR_Races
{
    public partial class MainWindow : Form
    {
        private Painter _painter;
        private RaceManager _raceManager;
        public MainWindow()
        {
            InitializeComponent();
            int maxX = mainPictureBox.Width;
            int maxY = mainPictureBox.Height;
            int straightLength = maxX / 2;
            int _turnRadius = maxX / 5;
            int pitPosY = maxY / 2 + maxX / 7;
            int turnCurveRadius = 0;
            int totalLength = (int)(maxX + 2 * 3.1415 * _turnRadius);
            int penCircuitSize = 60;
            int penCarSize=1;
            _raceManager = new(straightLength, _turnRadius, pitPosY, turnCurveRadius, penCircuitSize, penCarSize, mainPictureBox);
            _painter = new(_raceManager.Worldinformation);
            
            _painter.listOfCars = _raceManager.CreateListOfCars();

            programTimer.Interval = 1;//Interval of Timer executing event "Tick" (in milliseconds)
            programTimer.Tick += new EventHandler(RunRace);
            programTimer.Start();
            Thread tempThread = new(_raceManager.StartRace);
            tempThread.Start();
            this.FormClosing += MainWindowClosing_KillThreads;
        }

        //metoda od�wie�ania ekranu, wywo�ywana automatycznie, gdy system uwa�a, �e nale�y j� wywo�a�.
        //Alternatywnie u�y�: mainPictureBox.Invalidate() w razie gdyby by�o potrzebne
        private void mainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            _painter.PaintCircuit(e.Graphics);
            _painter.PaintCarsPosition(e.Graphics);
        }

        private void RunRace(object sender, EventArgs e)
        {
            mainPictureBox.Invalidate();
        }

        private void MainWindowClosing_KillThreads(object sender, FormClosingEventArgs e)
        {
            _raceManager.KillThreads();
        }

    }
}