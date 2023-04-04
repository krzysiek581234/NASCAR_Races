using System.Windows.Forms;
using System.Threading;

namespace NASCAR_Races
{
    public partial class MainWindow : Form
    {
        Painter painter;
        RaceManager raceManager;
        public MainWindow()
        {
            InitializeComponent();
            int maxX = mainPictureBox.Width;
            int maxY = mainPictureBox.Height;
            int straightLength = maxX / 2;
            int turnRadius = maxX / 5;
            int pitPosY = maxY / 2 + maxX / 7;
            int turnCurveRadius = 0;
            int totalLength = (int)(maxX + 2 * 3.1415 * turnRadius);
            raceManager = new(straightLength, turnRadius, pitPosY, turnCurveRadius, mainPictureBox);
            painter = new(maxX, maxY, straightLength, turnRadius, pitPosY);

            int numberOfCars = 1;
            painter.listOfCars = raceManager.CreateListOfCars(numberOfCars);

            programTimer.Interval = 1;//Interval of Timer executing event "Tick" (in milliseconds)
            programTimer.Tick += new EventHandler(RunRace);
            programTimer.Start();
        }

        //metoda od�wie�ania ekranu, wywo�ywana automatycznie, gdy system uwa�a, �e nale�y j� wywo�a�.
        //Alternatywnie u�y�: mainPictureBox.Invalidate() w razie gdyby by�o potrzebne
        private void mainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            painter.PaintCircuit(e.Graphics);
            painter.PaintCarsPosition(e.Graphics);
        }

        internal void RunRace(object sender, EventArgs e)
        {
            raceManager.MoveCars();
            mainPictureBox.Invalidate();
        }

    }
}