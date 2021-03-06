﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkProject
{
    public partial class GamePlayingScreen : Form
    {
        char[,] gameBoard;
        Dictionary<Point, int> Snakes;
        Dictionary<Point, int> Ladders;
        List<Client> Clients;
        List<Point> PlayersLocation;
        int myIndex;
        private static int groupPort = 8000;
        Socket currentPlayer;
        bool IsServer;
        Bitmap Board;
        int numberOfPlayers;
        UdpClient udp;
        public GamePlayingScreen(char[,] board, Dictionary<Point, int> snakes, Dictionary<Point, int> ladders, List<Client> clients, int numberOfPlayers, Socket me, bool Server, UdpClient udp_c)
        {
            InitializeComponent();
            Clients = clients;
            gameBoard = board;
            Snakes = snakes;
            Ladders = ladders;
            currentPlayer = me;
            this.numberOfPlayers = numberOfPlayers;

            PlayersLocation = new List<Point>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                PlayersLocation.Add(new Point(0, 0));
            }

            GeneratePlayerList(numberOfPlayers);
            IsServer = Server;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            DrawBoard();

            if (IsServer)
            {
                udp = udp_c;
                btnRollTheDice.Enabled = true;
                myIndex = 0;
                for (int i = 0; i < clients.Count; i++)
                {
                    Thread t = new Thread(new ParameterizedThreadStart(RecieveFromClients));
                    t.Start(clients[i]);
                }
                for (int i = 1; i < clients.Count; i++)
                {
                    byte[] clientMessage = new byte[50];
                    Console.WriteLine("Waiting to receive from client #" + i);
                    clients[i].playerSocket.Receive(clientMessage);
                    Console.WriteLine("Received from client #" + i + "\n" + Encoding.ASCII.GetString(clientMessage));

                }
                for (int i = 1; i < clients.Count; i++)
                {
                    clients[i].playerSocket.Send(Encoding.ASCII.GetBytes(clients[i].Rank.ToString()));
                }
            }
            else
            {
                Thread t = new Thread(RecieveFromServer);
                t.Start();
                Console.WriteLine("Sending join game notification to server");
                currentPlayer.Send(Encoding.ASCII.GetBytes("Client with IP " + GameStart.GetLocalIPAddress() + " joined the game"));
                Console.WriteLine("Sent join game notification to server");
                byte[] myIndexBytes = new byte[20];
                currentPlayer.Receive(myIndexBytes);
                Console.WriteLine("My index is " + Encoding.ASCII.GetString(myIndexBytes));
                myIndex = Int32.Parse(Encoding.ASCII.GetString(myIndexBytes));
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////DRAWING FUNCTIONS/////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////YOU DON'T NEED TO WRITE ANY CODE HERE///////////////////////////////////////////////////////////////////////////////////
        void GeneratePlayerList(int numberOfPlayers)
        {
            //maximum number of players is 8
            numberOfPlayers = numberOfPlayers > 8 ? 8 : numberOfPlayers;

            for (int i = 0; i < numberOfPlayers; i++)
            {
                Label label = new Label();
                label.AutoSize = true;
                label.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label.Location = new System.Drawing.Point(85, 65 + i * 50);
                label.Name = "label2";
                label.Size = new System.Drawing.Size(76, 19);
                label.TabIndex = 0;
                label.Text = "Player " + (i + 1);

                this.groupBox1.Controls.Add(label);

                PictureBox pictureBox = new PictureBox();
                pictureBox.Location = new System.Drawing.Point(30, 55 + i * 50);
                pictureBox.Name = "pictureBox2";
                pictureBox.Size = new System.Drawing.Size(48, 40);
                pictureBox.TabIndex = 0;
                pictureBox.TabStop = false;
                GeneratePlayerColor(i + 1);
                Image bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.FillEllipse(new SolidBrush(PlayerColors[i]), 0, 0, 48, 40);
                g.Flush();
                pictureBox.BackgroundImage = bmp;
                this.groupBox1.Controls.Add(pictureBox);

            }
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
        }
        List<Color> PlayerColors = new List<Color>();
        void GeneratePlayerColor(int index)
        {
            PlayerColors.Add(Color.FromArgb(index * 200 % 255, index * 300 % 255, index * 400 % 255));
        }
        void DrawBoard()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                    else
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                }
            }

            for (int i = 0; i < 11; i++)
            {
                g.DrawLine(Pens.Black, new Point(0, i * 50), new Point(500, i * 50));
                g.DrawLine(Pens.Black, new Point(i * 50, 0), new Point(i * 50, 500));
            }

            g.FillRectangle(Brushes.LightPink, new Rectangle(0, 0, 50, 50));
            g.FillRectangle(Brushes.LightPink, new Rectangle(0, 450, 50, 50));

            Bitmap snakeImg = new Bitmap("snake.png");
            foreach (var snake in Snakes)
            {
                g.DrawImage(snakeImg, snake.Key.X * 50, (9 - snake.Key.Y) * 50, 50, (snake.Value + 1) * 50);
            }
            Bitmap ladderImg = new Bitmap("ladder.png");
            foreach (var ladder in Ladders)
            {
                g.DrawImage(ladderImg, ladder.Key.X * 50, (9 - ladder.Key.Y - ladder.Value) * 50 + 25, 50, ladder.Value * 50 + 10);
            }

            g.DrawString("START", SystemFonts.DefaultFont, Brushes.Red, new PointF(5, 470));
            g.DrawString("END", SystemFonts.DefaultFont, Brushes.Red, new PointF(10, 20));
            Board = bmp;
            pictureBox1.BackgroundImage = bmp;
        }
        private void GamePlayingScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        void DrawAllPlayers()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);
            for (int i = 0; i < PlayersLocation.Count; i++)
            {
                g.FillEllipse(new SolidBrush(PlayerColors[i]), new Rectangle(PlayersLocation[i].X * 50, (9 - PlayersLocation[i].Y) * 50, 50 - i, 50 - i));
            }
            pictureBox1.Image = bmp;
        }

        private void GamePlayingScreen_Paint(object sender, PaintEventArgs e)
        {
            DrawAllPlayers();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////YOUR CODE HERE///////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void btnRollTheDice_Click(object sender, EventArgs e)
        {
            //write the button code here:
            //1- disable "RollTheDice" button
            //  btnRollTheDice.Enabled = false;
            //2- generate random number and write it in textbox
            Random ran = new Random();
            int move = ran.Next(1, 7);
            textBox1.Text = move.ToString();
            Point oldLocation = PlayersLocation[myIndex];
            Point newLocation = new Point(oldLocation.X, oldLocation.Y);

            if (newLocation.X - move < 0 && newLocation.Y >= 9)
            {
                return;
            }
            while (move != 0)
            {

                if (newLocation.Y % 2 == 0)
                {
                    if (newLocation.X < 9)
                    {
                        newLocation.X++;
                        move--;
                    }
                    else
                    {
                        newLocation.Y++;
                        move--;
                    }
                }
                else if (newLocation.Y % 2 != 0)
                {
                    if (newLocation.X > 0)
                    {
                        newLocation.X--;
                        move--;
                    }
                    else
                    {
                        newLocation.Y++;
                        move--;
                    }
                }


            }
            if (gameBoard[newLocation.Y, newLocation.X] == 'L')
            {
                newLocation.Y += Ladders[newLocation];
            }
            else if (gameBoard[newLocation.Y, newLocation.X] == 'S')
            {
                newLocation.Y -= Snakes[newLocation];
            }
            if (newLocation.X == 0 && newLocation.Y >= 9)
            {
                newLocation.X = 0;
                newLocation.Y = 9;
                String winnerIP = Clients[myIndex].IP;
                Console.WriteLine("Player # " + myIndex + "Is the winner\nIP : " + winnerIP);
                //WinningForm form = new WinningForm(myIndex, winnerIP);
                //this.Enabled = false;
                //form.Show();
                BroadCastTheWinnerIs(myIndex);
            }

            PlayersLocation[myIndex] = newLocation;
            //3- after 3 sec move the player coin
            //Thread.Sleep(3000);
            DrawAllPlayers();
            if (myIndex < numberOfPlayers - 1)
                myIndex++;
            else myIndex = 0;

            //4- check if new location is ladder or snake using gameBoard array and modify the new location based on the value of gameBoard[y,x] = 'S' or = 'L'

            //6- update the location of currentPlayer (to be modified in drawing)

            //TODO :
            if (IsServer)
            {
                //call BroadCastLocation(0) as the server index is always 0 in the client list
                // BroadCastLocation(0);

                //call BroadCastWhoseTurn(0) to see which player will play after server
                //BroadCastWhoseTurn(0);
            }
            else
            {
                //if final location is the winning location then call the function SendTheWinnerIsMeToServer()

                //else send the final location to server by calling SendLocationToServer()
            }

        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////CLIENT///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //TODO :
        void RecieveFromServer()
        {
            //currentPlayer
            //use the currentPlayer socket to recieve from the server

            //parse the recieved message


            //if turn message check if the IP matched with my IP
            //then check if currentPlayer boolean = true
            //enable "RollTheDice" button and play
            //else keep it disabled

            //if location message then update the location of player n
            //update client n location

            //if winning message
            //go to WinningForm with the playerNumber
        }
        //TODO :
        void SendLocationToServer()
        {
            //use the currentPlayer socket to send to server "PlayersLocation[myIndex]"
            //message should look like this:
            //IP#PlayersLocation[myIndex]#
            currentPlayer.Send(Encoding.ASCII.GetBytes(GameStart.GetLocalIPAddress() + "#" + PlayersLocation[myIndex] + "#"));
        }
        //TODO :
        void SendTheWinnerIsMeToServer()
        {
            //use the currentPlayer socket to send to server the winner message
            //message should look like this:
            //IP#
            currentPlayer.Send(Encoding.ASCII.GetBytes(GameStart.GetLocalIPAddress() + "#"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////SERVER///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecieveFromClients(Object client)
        {
            Client c = (Client)client;
            //recieve message and parse it

            //if Winning Message
            //call BroadCastTheWinnerIs(playerNumber)
            //go to WinningForm

            //if LocationMessage
            //call BraodCastLocation(player number)
            //call BroadCastWhoseTurn(player number)
        }

        void BroadCastLocation(int playerNumber)
        {
            //here send the mssage to all clients, containing the location of PlayersLocation[playerNumber] and attach its IP and playerNumber
        }

        void BroadCastWhoseTurn(int playerNumber)
        {
            //see in the client list which 1 has the turn to play after playerNumber
            //here send the message to all clients, containing the IP only
        }
        void BroadCastTheWinnerIs(int playerNumber)
        {
            Console.WriteLine("winning player is ........... ");
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            string winner_msg = "Winner#" + Clients[playerNumber].IP + "#"+Clients[playerNumber].Rank;
            byte[] WinnerIp_msg = Encoding.ASCII.GetBytes(winner_msg);
            Console.WriteLine(WinnerIp_msg + "         " + winner_msg);
            udp.Send(WinnerIp_msg, WinnerIp_msg.Length, endPoint);
        }

        private void GamePlayingScreen_Load(object sender, EventArgs e)
        {

        }
    }
}
