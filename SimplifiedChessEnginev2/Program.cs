// Author       Thomas Spatz
// Date         03.31.2024
// Challenge    https://www.hackerrank.com/challenges/simplified-chess-engine/

namespace SimplifiedChessEnginev2;

class Solution
{
    // t = part:
    //      Q = Queen
    //      N = Knight
    //      B = Bishop
    //      R = Rook
    
    // c = column (A,B,C,D) (I used numbers for this to make my life easier)
    // r = row    (1,2,3,4)
    
    
    // Constraints
    // Board Width = 4
    // Board Height = 4
    // 1 <= g <= 200   (MAX GAMES = 200)
    // 1 <= w,b <= 5   (MAX PIECES = 5 ea)
    // 1 <= m <= 6     (MAX LOOK AHEAD ROUNDS = 6)
    // ONE QUEEN, max 2 R, max 2 B+R
    
    // Board:
    //     0   1   2   3
    //     A   B   C   D
    //   -----------------
    // 0 |   |   |   |   |
    //   -----------------
    // 1 |   |   |   |   |
    //   -----------------
    // 2 |   |   |   |   |
    //   -----------------
    // 3 |   |   |   |   |
    //   -----------------
    
    private enum Team
    {
        Black,
        White,
        None // Empty tile
    };

    private enum Type
    {
        Queen,
        Knight,
        Bishop,
        Rook,
        None // Empty tile
    }

    private struct Coordinate
    {
        public int col;
        public int row;
    }

    private struct Move
    {
        public Coordinate fromPos;
        public Type fromPiece;
        public Team fromTeam;
        
        public Coordinate toPos;
        public Type toPiece;
        public Team toTeam;
    }

    private struct Piece
    {
        public Type type;
        public Team team;
        public Coordinate pos;
    }

    private struct MovementVectors
    {
        public int[] colVectors;
        public int[] rowVectors;
    }

    // MovementVectors used in pairs of two ( colVectors[0] with rowVectors[0],... )
    // Note: Grid starts at top left, going down = y increment, going right = x increment
    //       => UP = y-1 and x,... 
    private static Dictionary<Type, MovementVectors> movementVectors = new()
    {
        {
            Type.Queen, // Vertical, Horizontal and Diagonal Movement
            new MovementVectors
            {
                // UP, UP RIGHT, RIGHT, DOWN RIGHT, DOWN, DOWN LEFT, LEFT, UP LEFT
                colVectors = new[] {  0,  1, 1, 1, 0, -1, -1, -1 },
                rowVectors = new[] { -1, -1, 0, 1, 1,  1,  0, -1 }
            }
        },
        {
            Type.Bishop, // Only Diagonal Movement
            new MovementVectors
            {
                // UP RIGHT, DOWN RIGHT, DOWN LEFT, UP LEFT
                colVectors = new[] {  1, 1, -1, -1 },
                rowVectors = new[] { -1, 1,  1, -1 }
            }
        },
        {
            Type.Rook, // Vertical and Horizontal Movement
            new MovementVectors
            {
                // UP, RIGHT, DOWN, LEFT
                colVectors = new[] {  0, 1, 0, -1 },
                rowVectors = new[] { -1, 0, 1,  0 }
            }
        }
    };
    
    // Snapshot of the board for better piece handling
    private class Snapshot
    {
        public Coordinate blackQueenPos;
        public Coordinate whiteQueenPos;
        
        public List<Piece> blacks = new();
        public List<Piece> whites = new();
    }
    
    private static void Main(string[] args) {
        
        // HackerRank, why can you not use something normal?
        TextWriter textWriter = new StreamWriter(@System.Environment.GetEnvironmentVariable("OUTPUT_PATH")!, true);
        
        // Uncomment for debugging locally
        // TextWriter textWriter = new StreamWriter(Console.OpenStandardOutput());

        // Number of games
        var g = Convert.ToInt32(Console.ReadLine());

        for (var gItr = 0; gItr < g; gItr++) {
            // Read a line
            var wbm = Console.ReadLine()!.Split(' ');

            // Number of initial white pieces
            var w = Convert.ToInt32(wbm[0]);

            // Number of initial black pieces
            var b = Convert.ToInt32(wbm[1]);

            // Number of maximum moves
            var m = Convert.ToInt32(wbm[2]);

            // White pieces
            var whites = new char[w][];
            for (var whitesRowItr = 0; whitesRowItr < w; whitesRowItr++) {
                whites[whitesRowItr] = Array.ConvertAll(Console.ReadLine()!.Split(' '), whitesTemp => whitesTemp[0]);
            }

            // Black pieces
            var blacks = new char[b][];
            for (var blacksRowItr = 0; blacksRowItr < b; blacksRowItr++) {
                blacks[blacksRowItr] = Array.ConvertAll(Console.ReadLine()!.Split(' '), blacksTemp => blacksTemp[0]);
            }
            
            // Construct chessboard
            var chessBoard = new char[4][];
            for (var i = 0; i < 4; i++)
            {
                chessBoard[i] = new char[4];
            }
            
            // Uppercase Letter = White
            // Lowercase Letter = Black
            
            // Map the A-Z columns from 0-4
            // Map the rows from 0-4 and flip horizontally
            // Note: Input in char! To convert to numerics, we have to subtract their base value (see ASCII table)
            foreach (var piece in whites)
            {
                var type = piece[0];
                var col = piece[1] - 'A';
                var row = 4 - (piece[2] - '0');
                chessBoard[row][col] = type;
            }
            foreach (var piece in blacks)
            {
                var type = char.ToLower(piece[0]);
                var col = piece[1] - 'A';
                var row = 4 - (piece[2] - '0');
                chessBoard[row][col] = type;
            }
            
            // YES / NO
            var result = SimplifiedChessEngine(chessBoard, m);
            textWriter.WriteLine(result);
        }

        // Flush da toilet
        textWriter.Flush();
        textWriter.Close();
    }

    private static string SimplifiedChessEngine(char[][] board, int moves)
    {
        return EngineRun(board, moves, 0) ? "YES" : "NO";
    }

    // Recursive Engine
    private static bool EngineRun(char[][] board, int moves, int currentMove)
    {
        // First we snapshot the board
        var snapshot = MakeSnapshot(board);

        // If the Black Queen is in danger at the beginning of White's turn -> WIN
        if (currentMove % 2 == 0)
        {
            if (BlackQueenInDanger(board, snapshot)) return true;
        }

        // No more moves left -> Lose
        if (currentMove >= moves-1) return false;
        
        if (currentMove % 2 == 0) // White's Turn
        {
            // Start DFS search for possible endings
            
            // We want to try every possible move, every round
            foreach (var move in GetMoves(board, snapshot.whites))
            {
                // Make available move
                MakeMove(board, move);
                var newSnapshot = MakeSnapshot(board);

                // If we endangered ourselves (invalid move), go back
                if (WhiteQueenInDanger(board, newSnapshot))
                {
                    UndoMove(board, move);
                    continue;
                }
                
                // Otherwise extend recursive call
                // If true is returned here, we found a winning route!
                if (EngineRun(board, moves, currentMove + 1))
                {
                    UndoMove(board, move);
                    return true;
                }
                
                // Go back to previous spot to try another approach
                UndoMove(board, move);
            }
        }
        else // Black's Turn 
        {
            foreach (var move in GetMoves(board, snapshot.blacks))
            {
                MakeMove(board, move);
                
                // If we found a winning condition for black (out of moves) -> Lose
                if (!EngineRun(board, moves, currentMove + 1))
                {
                    UndoMove(board, move);
                    return false;
                }
                
                // We will end up here if White would win, so we try a different move
                UndoMove(board, move);
            }

            // Black has no moves left (e.g.: Stalemate) -> White wins!
            return true;
        }

        return false;
    }
    
    // Empty field = No Team
    // Uppercase Letter = White Team
    // Lowercase Letter = Black Team
    private static Team GetTeam(char piece)
    {
        return piece == (char)0 ? Team.None : char.IsLower(piece) ? Team.Black : Team.White;
    }

    // Convert between type and char
    private static Type GetType(char piece)
    {
        return char.ToUpper(piece) switch
        {
            'Q' => Type.Queen,
            'N' => Type.Knight,
            'B' => Type.Bishop,
            'R' => Type.Rook,
            _ => Type.None // default
        };
    }
    
    private static char FromType(Type type, Team team)
    {
        return type switch
        {
            Type.Queen => team == Team.White ? 'Q' : 'q',
            Type.Knight => team == Team.White ? 'N' : 'n',
            Type.Bishop => team == Team.White ? 'B' : 'b',
            Type.Rook => team == Team.White ? 'R' : 'r',
            _ => (char)0 // default
        };
    }

    private static Snapshot MakeSnapshot(char[][] board)
    {
        var snapshot = new Snapshot();

        // Go over the entire board
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                // Skip empty tiles
                if(board[i][j] == (char)0) continue;
                
                // Initialize pieces
                var boardPiece = board[i][j];
                var piece = new Piece
                {
                    team = GetTeam(boardPiece),
                    type = GetType(boardPiece),
                    pos = new Coordinate { row = i, col = j }
                };

                // Add the pieces to the corresponding teams and set queen position for optimization
                if (piece.team == Team.White)
                {
                    snapshot.whites.Add(piece);
                    if (piece.type != Type.Queen) continue;

                    snapshot.whiteQueenPos = new Coordinate { col = piece.pos.col, row = piece.pos.row };
                }
                else
                {
                    snapshot.blacks.Add(piece);
                    if (piece.type != Type.Queen) continue;
                    
                    snapshot.blackQueenPos = new Coordinate { col = piece.pos.col, row = piece.pos.row };
                }
                
            }
        }

        return snapshot;
    }
    
    // Returns all available moves a Team has, this includes:
    //      - Moving to good spots (empty)
    //      - Moving to occupied spots of the other team (take piece)
    //      - Moving to bad spots (dangerous for piece)
    private static List<Move> GetMoves(char[][] board, List<Piece> pieces)
    {
        // No pieces = No Moves bro
        if (pieces.Count <= 0) return new List<Move>();
        var moves = new List<Move>();

        // Go over the entire board
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                // We will not target our own pieces
                if(GetTeam(board[i][j]) == pieces[0].team) continue;

                // For every piece in our team, check if they can reach said tile
                // If so, add them to a new possible Move
                foreach (var piece in pieces)
                {
                    if (CanTarget(board, piece, new Coordinate { row = i, col = j }))
                    {
                        var move = new Move
                        {
                            fromPiece = piece.type,
                            fromPos = piece.pos,
                            fromTeam = piece.team,
                            
                            toPiece = GetType(board[i][j]),
                            toPos = new Coordinate { row = i, col = j },
                            toTeam = GetTeam(board[i][j])
                        };
                        
                        moves.Add(move);
                    }
                }
            }
        }

        return moves;
    }

    // Returns whether a piece is able to reach a specific spot on the board in one move or not
    // Note: Pieces can NOT walk through other pieces (except Knight, they can jump over others)
    private static bool CanTarget(char[][] board, Piece piece, Coordinate target)
    {
        // Knight needs special treatment since they can jump...
        if (piece.type == Type.Knight)
        {
            // L shaped jump in every direction (2 instead of 4 calculations thanks to Abs)
            return Math.Abs(piece.pos.row - target.row) == 2 && Math.Abs(piece.pos.col - target.col) == 1 ||
                   Math.Abs(piece.pos.row - target.row) == 1 && Math.Abs(piece.pos.col - target.col) == 2;
        }
        
        // Retrieve the movement vectors for our piece type from the dictionary
        movementVectors.TryGetValue(piece.type, out var vectors);
        
        for (var vectorIndex = 0; vectorIndex < vectors.colVectors.Length; vectorIndex++)
        {
            // Start at current position + one step in the first vector direction
            // Note: There is no need to check the current position since well... we are already here
            var colWalk = piece.pos.col + vectors.colVectors[vectorIndex];
            var rowWalk = piece.pos.row + vectors.rowVectors[vectorIndex];

            // As long as we are on the board, keep walking in the vector direction
            for (; colWalk >= 0 && colWalk <= 3 && rowWalk >= 0 && rowWalk <= 3; colWalk += vectors.colVectors[vectorIndex], rowWalk += vectors.rowVectors[vectorIndex])
            {
                // If we are NOT at our destination yet but we bump into someone, goal is not reachable
                if (colWalk != target.col || rowWalk != target.row)
                {
                    if (board[rowWalk][colWalk] != 0) break;
                }

                // We arrived at the target location! (that means it is reachable...)
                if (colWalk == target.col && rowWalk == target.row) return true;
            }
        }

        return false;
    }

    // Whether any black piece can target the White Queen's position
    private static bool WhiteQueenInDanger(char[][] board, Snapshot snapshot)
    {
        return snapshot.blacks.Any(piece => CanTarget(board, piece, snapshot.whiteQueenPos));
    }
    
    // Same thing for other team
    private static bool BlackQueenInDanger(char[][] board, Snapshot snapshot)
    {
        return snapshot.whites.Any(piece => CanTarget(board, piece, snapshot.blackQueenPos));
    }

    // Move to a position and replace our old position with an empty tile
    private static void MakeMove(char[][] board, Move move)
    {
        board[move.toPos.row][move.toPos.col] = FromType(move.fromPiece, move.fromTeam);
        board[move.fromPos.row][move.fromPos.col] = (char)0;
    }
    
    // Hell nah, go back!! CTRL+Z!! CTRL+Z!!!!!
    private static void UndoMove(char[][] board, Move move)
    {
        board[move.toPos.row][move.toPos.col] = FromType(move.toPiece, move.toTeam);
        board[move.fromPos.row][move.fromPos.col] = FromType(move.fromPiece, move.fromTeam);
    }
}
