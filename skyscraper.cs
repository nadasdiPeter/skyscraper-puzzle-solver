using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CodeWars
{
   class Program
   {
      public class Riddle
      {
         public class Cell
         {
            public int Row { get; set; }
            public int Col { get; set; }

            public Cell(int x, int y) => (Row, Col) = (x, y);

            public void Set_Possibilities(int[] p) { Possibilities[Row][Col] = p;  }
            public int[] Get_Possibilities() => Possibilities[Row][Col];
            public bool IsFinished() => 1 == Get_Possibilities().Count();
            public bool IsCluePossible(int clue) => Get_Possibilities().Contains(clue);

            public void RemoveClue(int clue)
            {
               if (IsCluePossible(clue) && IsFinished() == false)
               {
                  var t = Get_Possibilities().ToList();
                  t.Remove(clue);
                  Set_Possibilities(t.ToArray());
               }
            }
         }

         public class Sequence
         {
            public int Clue_A { get; set; }
            public int Clue_B { get; set; }
            public bool Finished { get; set; }
            public List<Cell> Cells { get; set; }
            public List<int> Unfinished_Clues { get; set; } = Range.ToList();

            public Sequence(int clueA, int clueB, List<Cell> members) => (Clue_A, Clue_B, Cells, Finished) = (clueA, clueB, members, false);
            public void FinishedClue(int clue)
            {
               Unfinished_Clues.Remove(clue);
               if (Unfinished_Clues.Count == 0) Finished = true;
            }
            public bool IsContainsAnyClue() => (Clue_A != 0 || Clue_B != 0);
            public int GetClueCountInSequence(int clue) => Cells.Where(c => c.IsCluePossible(clue) == true).Count();
            public void RemoveClueFromCells(int clue)
            {
               FinishedClue(clue);
               foreach (Cell c in Cells)
                  c.RemoveClue(clue);
            }
            public List<Cell> GetCellsContainingClue(int clue) => Cells.Where(c => c.Get_Possibilities().Contains(clue)).ToList();
            public int GetFinishedCellCount() => Cells.Where(c => c.IsFinished() == true).Count();
         }

         public int MatrixSize { get; set; }
         public List<Sequence> Sequences { get; set; } = new List<Sequence>();
         private static int[][][] Possibilities;
         bool PossibilityMatrixChanged { get; set; } = false;
         public int[][] Solution { get; }
         private static int[] Range;
         Func<Sequence, bool> Verify_FuncPointer = null;

         public void CopyMatrix( ref int[][][] source, ref int[][][] target )
         {
            target = new int[MatrixSize][][];
            for( int row = 0; row < MatrixSize; row++ )
            {
               target[row] = new int[MatrixSize][];
               for(int col = 0; col< MatrixSize; col++)
               {
                  target[row][col] = source[row][col];
               }
            }
         }

         public Riddle(int[] clues)
         {
            /* Initialize Size & Range */
            MatrixSize = clues.Count() / 4;
            Range = Enumerable.Range(1, MatrixSize).ToArray();

            /* Decide verification function pointer */
            switch (MatrixSize)
            {
               case 4:
                  Verify_FuncPointer = Verify_4x4;
                  break;
               case 6:
                  Verify_FuncPointer = Verify_6x6;
                  break;
               case 7:
                  Verify_FuncPointer = Verify_7x7;
                  break;
               case 8:
                  Verify_FuncPointer = Verify_8x8;
                  break;
               default:
                  throw new InvalidOperationException("Matrix size is not supported!");
            }

            /* Initialization of possibility & solution matrix */
            Possibilities = new int[MatrixSize][][];
            Solution = new int[MatrixSize][];
            for (int row = 0; row < MatrixSize; row++)
            {
               Solution[row] = new int[MatrixSize]; // Initialized with all zeros.
               Possibilities[row] = new int[MatrixSize][];
               for (int col = 0; col < MatrixSize; col++)
                  Possibilities[row][col] = Range; // Initialized with all possible values 1 .. SIZE
            }

            /* Initialization of COLUMBs */
            for (int col = 0; col < MatrixSize; col++)
            {
               List<Cell> members = new List<Cell>();
               for (int row = 0; row < MatrixSize; row++)
                  members.Add(new Cell(row, col));
               Sequence s = new Sequence(clues[col], clues[((3 * MatrixSize) - 1) - col], members);
               Sequences.Add(s);
            }

            /* Initialization of ROWs */
            for (int row = 0; row < MatrixSize; row++)
            {
               List<Cell> members = new List<Cell>();
               for (int col = 0; col < MatrixSize; col++)
                  members.Add(new Cell(row, col));
               Sequence s = new Sequence(clues[((4 * MatrixSize) - 1) - row], clues[MatrixSize + row], members);
               Sequences.Add(s);
            }

            /* Trigger Solver */
            Solve();
         }

         List<Sequence> GetCols() => Sequences.GetRange(0, MatrixSize);
         List<Sequence> GetRows() => Sequences.GetRange(MatrixSize, MatrixSize);

         public bool IsSolved() => Sequences.Where(s => s.Finished == true).Count() == Sequences.Count();

         public bool IsSolvable()
         {
            foreach (Sequence s in Sequences)
               if (Verify_FuncPointer(s) == false)
                  return false;
            return true;
         }

         public void SetClue(Cell c, int clue)
         {
            if (Solution[c.Row][c.Col] == 0)
            {
               Sequences[c.Col].RemoveClueFromCells(clue);
               Sequences[MatrixSize + c.Row].RemoveClueFromCells(clue);
               c.Set_Possibilities(new int[] { clue });
               PossibilityMatrixChanged = true;
               Solution[c.Row][c.Col] = clue;
            }
         }

         public bool Verify(int clue, List<int> cells)
         {
            if (cells.Distinct().Count() == MatrixSize)
            {
               int insight = 1;
               int highest_building = cells[0];
               for ( int i = 1; i < MatrixSize; i++ )
                  if (cells[i] > highest_building)
                  {
                     insight++;
                     highest_building = cells[i];
                  }
               return (clue == insight);
            }
            else // Not valid combination because not all the buildings are different 
               return false;
         }

         public bool Verify_8x8(Sequence s)
         {
            List<int> sum = new List<int>();
            foreach (Cell c in s.Cells)
               sum.AddRange(c.Get_Possibilities().ToList());
            if (sum.Distinct().Count() != MatrixSize) return false;

            if (s.IsContainsAnyClue() == false)
               return true; // No reason to iterate further because there are no clues to compare.

            foreach (int a in s.Cells[0].Get_Possibilities())
               foreach (int b in s.Cells[1].Get_Possibilities())
                  if (b != a)
                     foreach (int c in s.Cells[2].Get_Possibilities())
                        if (c != a && c != b)
                           foreach (int d in s.Cells[3].Get_Possibilities())
                              if (d != a && d != b && d != c)
                                 foreach (int e in s.Cells[4].Get_Possibilities())
                                    if (e != a && e != b && e != c && e != d)
                                       foreach (int f in s.Cells[5].Get_Possibilities())
                                          if (f != a && f != b && f != c && f != d && f != e)
                                             foreach (int g in s.Cells[6].Get_Possibilities())
                                                if (g != a && g != b && g != c && g != d && g != e && g != f)
                                                   foreach (int h in s.Cells[7].Get_Possibilities())
                                                      if (h != a && h != b && h != c && h != d && h != e && h != f && h != g)
                                                      {
                                                         bool A = (s.Clue_A != 0) ? Verify(s.Clue_A, new List<int>() { a, b, c, d, e, f, g, h }) : true;
                                                         bool B = (s.Clue_B != 0) ? Verify(s.Clue_B, new List<int>() { h, g, f, e, d, c, b, a }) : true;

                                                         if (A == true && B == true)
                                                            return true; // There is at least one valid combination.
                                                }
            return false; // No valid combination found.
         }

         public bool Verify_7x7(Sequence s)
         {
            List<int> sum = new List<int>();
            foreach (Cell c in s.Cells)
               sum.AddRange(c.Get_Possibilities().ToList());
            if (sum.Distinct().Count() != MatrixSize) return false;

            if (s.IsContainsAnyClue() == false)
               return true; // No reason to iterate further because there are no clues to compare.

            foreach(int a in s.Cells[0].Get_Possibilities())
               foreach (int b in s.Cells[1].Get_Possibilities())
                  if( b != a )
                     foreach (int c in s.Cells[2].Get_Possibilities())
                        if ( c != a && c != b)
                           foreach (int d in s.Cells[3].Get_Possibilities())
                              if (d != a && d != b && d != c )
                                 foreach (int e in s.Cells[4].Get_Possibilities())
                                    if (e != a && e != b && e != c && e != d)
                                       foreach (int f in s.Cells[5].Get_Possibilities())
                                          if (f != a && f != b && f != c && f != d && f != e)
                                             foreach (int g in s.Cells[6].Get_Possibilities())
                                                if (g != a && g != b && g != c && g != d && g != e && g != f)
                                                {
                                                   bool A = (s.Clue_A != 0) ? Verify(s.Clue_A, new List<int>() { a, b, c, d, e, f, g }) : true;
                                                   bool B = (s.Clue_B != 0) ? Verify(s.Clue_B, new List<int>() { g, f, e, d, c, b, a }) : true;

                                                   if (A == true && B == true)
                                                      return true; // There is at least one valid combination.
                                                }
            return false; // No valid combination found.
         }

         public bool Verify_6x6(Sequence s)
         {
            List<int> sum = new List<int>();
            foreach (Cell c in s.Cells)
               sum.AddRange(c.Get_Possibilities().ToList());
            if (sum.Distinct().Count() != MatrixSize) return false;

            if (s.IsContainsAnyClue() == false)
               return true; // No reason to iterate further.

            foreach (int a in s.Cells[0].Get_Possibilities())
               foreach (int b in s.Cells[1].Get_Possibilities())
                  if (b != a)
                     foreach (int c in s.Cells[2].Get_Possibilities())
                        if (c != a && c != b)
                           foreach (int d in s.Cells[3].Get_Possibilities())
                              if (d != a && d != b && d != c)
                                 foreach (int e in s.Cells[4].Get_Possibilities())
                                    if (e != a && e != b && e != c && e != d)
                                       foreach (int f in s.Cells[5].Get_Possibilities())
                                          if (f != a && f != b && f != c && f != d && f != e)
                                          {
                                             bool A = (s.Clue_A != 0) ? Verify(s.Clue_A, new List<int>() { a, b, c, d, e, f }) : true;
                                             bool B = (s.Clue_B != 0) ? Verify(s.Clue_B, new List<int>() { f, e, d, c, b, a }) : true;

                                             if (A == true && B == true)
                                                return true; // There is at least one valid combination.
                                          }
            return false; // No valid combination found.
         }

         public bool Verify_4x4(Sequence s)
         {
            List<int> sum = new List<int>();
            foreach (Cell c in s.Cells)
               sum.AddRange(c.Get_Possibilities().ToList());
            if (sum.Distinct().Count() != MatrixSize) return false;

            if (s.IsContainsAnyClue() == false)
               return true; // No reason to iterate further because there are no clues to compare.

            foreach (int a in s.Cells[0].Get_Possibilities())
               foreach (int b in s.Cells[1].Get_Possibilities())
                  if (b != a)
                     foreach (int c in s.Cells[2].Get_Possibilities())
                        if (c != a && c != b)
                           foreach (int d in s.Cells[3].Get_Possibilities())
                              if (d != a && d != b && d != c)
                              {
                                 bool A = (s.Clue_A != 0) ? Verify(s.Clue_A, new List<int>() { a, b, c, d }) : true;
                                 bool B = (s.Clue_B != 0) ? Verify(s.Clue_B, new List<int>() { d, c, b, a }) : true;

                                 if (A == true && B == true)
                                    return true; // There is at least one valid combination.
                              }
            return false; // No valid combination found.
         }

         public void FindSingleClues()
         {
            foreach (Sequence s in Sequences)
            {
               foreach (int clue in Range)
               {
                  List<Cell> cells = s.GetCellsContainingClue(clue);
                  if (cells.Count() == 1)
                     SetClue(cells[0], clue);
               }
               foreach (Cell cell in s.Cells)
                  if (cell.IsFinished())
                     SetClue(cell,cell.Get_Possibilities()[0]);
            }
            foreach (Sequence s in Sequences)
               if( (s.Finished == false) && (s.GetFinishedCellCount() == MatrixSize)) 
                     s.Finished = true;
         }

         public void TestCellValuesAgainstClues()
         {
            foreach (Sequence s in Sequences)
               if (s.IsContainsAnyClue())
                  foreach (Cell cell in s.Cells)
                     if (cell.IsFinished() == false)
                        foreach( int tested_clue in cell.Get_Possibilities())
                        {
                           int[] original_possibilities = cell.Get_Possibilities();
                           int[] temp = new int[] { tested_clue };
                           cell.Set_Possibilities(temp);

                           if (Verify_FuncPointer(s) == false) // no possible solution found
                           {
                              var o = original_possibilities.ToList();
                              o.Remove(tested_clue);
                              original_possibilities = o.ToArray();
                              PossibilityMatrixChanged = true;

                              if (original_possibilities.Count() == 1)
                                 SetClue(cell, original_possibilities[0]);
                           }

                           cell.Set_Possibilities(original_possibilities);
                        }
         }

         public void Print_Possibilities()
         {
            for (int i = 0; i < MatrixSize; i++)
            {
               if(i==0)
               {
                  foreach (Sequence s in GetCols())
                     Console.Write("         [" + s.Clue_A + "]   ");
                  Console.WriteLine();
               }
               Console.Write("[" + Sequences[MatrixSize + i].Clue_A + "]   ");
               for (int j = 0; j < MatrixSize; j++)
               {
                  for (int z = 0; z < Possibilities[i][j].Length; z++)
                  {
                     Console.Write(Possibilities[i][j][z] + ",");
                  }
                  for (int spaces = Possibilities[i][j].Length * 2; spaces < 15; spaces++)
                     Console.Write(" ");
               }
               Console.Write("   [" + Sequences[MatrixSize + i].Clue_B + "]");
               Console.WriteLine();
               if (i == MatrixSize-1)
               {
                  foreach (Sequence s in GetCols())
                     Console.Write("         [" + s.Clue_B + "]   ");
                  Console.WriteLine();
               }
            }
            for (int spaces = 0; spaces < 15 * 7; spaces++)
               Console.Write("-");

            Console.WriteLine();
            Console.ReadLine();
         }

         public void Print_Solution()
         {
            for (int i = 0; i < MatrixSize; i++)
            {
               for (int j = 0; j < MatrixSize; j++)
                  if(Solution[i][j] > 0)
                     Console.Write(Solution[i][j] + ", ");
                  else
                     Console.Write(" , ");
               Console.WriteLine();
            }
            Console.WriteLine();
         }

         private bool Solve()
         {
            while (IsSolved() == false)
            {
               PossibilityMatrixChanged = false;
               TestCellValuesAgainstClues();
               FindSingleClues();

               if (IsSolvable() == false)
                  return false;

               if (!PossibilityMatrixChanged)
               {
                  // Riddle is still solvable but no further clue can be rejected from the cells without guesses.
                  // Picking a cell value to make progress...
                  int least_unsolved_possibility = MatrixSize;
                  foreach(Sequence s in Sequences)
                     foreach(Cell c in s.Cells)
                        if((c.Get_Possibilities().Count() < least_unsolved_possibility) && (c.Get_Possibilities().Count() != 1))
                        {
                           least_unsolved_possibility = c.Get_Possibilities().Count();
                        }

                  foreach (Sequence s in Sequences)
                     foreach (Cell c in s.Cells)
                        if (c.Get_Possibilities().Count() == least_unsolved_possibility)
                        {
                           foreach(int p in c.Get_Possibilities())
                           {
                              int[][][] original_possibilities = null;
                              CopyMatrix(ref Possibilities, ref original_possibilities);

                              int[] original = c.Get_Possibilities();
                              int[] temp = new int[] { p };
                              c.Set_Possibilities(temp);

                              if (Solve() == false) 
                              {
                                 // No possible solution found, -> retrive the original state of the possibilty matrix.
                                 CopyMatrix(ref original_possibilities, ref Possibilities);
                              }
                              else
                              {
                                 // Solution found, but make a doublecheck on it before return. (Can be optimised if needed)
                                 if (IsSolvable() == false)
                                    return false;
                                 else
                                    return true; // We have the solution
                              }
                           }
                        }
               }
            }

            if (IsSolved())
            {
               for (int row = 0; row < MatrixSize; row++)
                  for (int col = 0; col < MatrixSize; col++)
                     Solution[row][col] = Possibilities[row][col][0];
               return true;
            }
            else
               return false;
         }
      }

      public int[][] SolvePuzzle(int[] clues)
      {
         Riddle r = new Riddle(clues);
         if (r.IsSolved())
            r.Print_Solution();
         else
            Console.WriteLine("Riddle has no possible solution!\n");

         return r.Solution;
      }

      public static void Main()
      {
         var clues_8x8_hard = new[] { 0, 0, 2, 0, 4, 0, 3, 0, 2, 5, 2, 2, 2, 0, 4, 0, 1, 4, 2, 0, 4, 0, 0, 5, 0, 2, 4, 0, 3, 0, 0, 3 }; // There are more solution.

         var clues1_8x8 = new[] { 4, 3, 4, 1, 5, 4, 3, 2, 2, 1, 2, 3, 2, 4, 3, 3, 4, 5, 3, 1, 4, 2, 4, 2, 2, 3, 1, 3, 2, 5, 3, 3 }; // There are more solution.

         var clues2_8x8 = new[] { 4, 3, 4, 1, 5, 4, 3, 2, 2, 1, 2, 3, 2, 4, 3, 3, 4, 5, 3, 1, 4, 2, 5, 2, 2, 3, 5, 3, 2, 5, 3, 3 };

         var clues1_7x7 = new[] { 7, 0, 0, 0, 2, 2, 3, 0, 0, 3, 0, 0, 0, 0, 3, 0, 3, 0, 0, 5, 0, 0, 0, 0, 0, 5, 0, 4 };

         var clues2_7x7 = new[] { 0, 2, 3, 0, 2, 0, 0, 5, 0, 4, 5, 0, 4, 0, 0, 4, 2, 0, 0, 0, 6, 5, 2, 2, 2, 2, 4, 1 };

         var expected1_7x7 = new[] { new[] { 1, 5, 6, 7, 4, 3, 2 },
                                     new[] { 2, 7, 4, 5, 3, 1, 6 },
                                     new[] { 3, 4, 5, 6, 7, 2, 1 },
                                     new[] { 4, 6, 3, 1, 2, 7, 5 },
                                     new[] { 5, 3, 1, 2, 6, 4, 7 },
                                     new[] { 6, 2, 7, 3, 1, 5, 4 },
                                     new[] { 7, 1, 2, 4, 5, 6, 3 } };

         var expected2_7x7 = new[] { new[] { 7, 6, 2, 1, 5, 4, 3 },
                                     new[] { 1, 3, 5, 4, 2, 7, 6 },
                                     new[] { 6, 5, 4, 7, 3, 2, 1 },
                                     new[] { 5, 1, 7, 6, 4, 3, 2 },
                                     new[] { 4, 2, 1, 3, 7, 6, 5 },
                                     new[] { 3, 7, 6, 2, 1, 5, 4 },
                                     new[] { 2, 4, 3, 5, 6, 1, 7 } };

         var clues1_6x6 = new[]{ 3, 2, 2, 3, 2, 1, 1, 2, 3, 3, 2, 2, 5, 1, 2, 2, 4, 3, 3, 2, 1, 2, 2, 4};

         var clues2_6x6 = new[]{ 0, 0, 0, 2, 2, 0, 0, 0, 0, 6, 3, 0, 0, 4, 0, 0, 0, 0, 4, 4, 0, 3, 0, 0};

         var clues3_6x6 = new[]{ 0, 3, 0, 5, 3, 4, 0, 0, 0, 0, 0, 1, 0, 3, 0, 3, 2, 3, 3, 2, 0, 3, 1, 0};

         var expected1_6x6 = new[]{new []{ 2, 1, 4, 3, 5, 6 },
                                   new []{ 1, 6, 3, 2, 4, 5 },
                                   new []{ 4, 3, 6, 5, 1, 2 },
                                   new []{ 6, 5, 2, 1, 3, 4 },
                                   new []{ 5, 4, 1, 6, 2, 3 },
                                   new []{ 3, 2, 5, 4, 6, 1 }};

         var expected2_6x6 = new[]{new []{ 5, 6, 1, 4, 3, 2 },
                                   new []{ 4, 1, 3, 2, 6, 5 },
                                   new []{ 2, 3, 6, 1, 5, 4 },
                                   new []{ 6, 5, 4, 3, 2, 1 },
                                   new []{ 1, 2, 5, 6, 4, 3 },
                                   new []{ 3, 4, 2, 5, 1, 6 }};

         var expected3_6x6 = new[]{new []{ 5, 2, 6, 1, 4, 3 },
                                   new []{ 6, 4, 3, 2, 5, 1 },
                                   new []{ 3, 1, 5, 4, 6, 2 },
                                   new []{ 2, 6, 1, 5, 3, 4 },
                                   new []{ 4, 3, 2, 6, 1, 5 },
                                   new []{ 1, 5, 4, 3, 2, 6 }};

         var clues1_4x4 = new int[] { 0, 0, 1, 2, 0, 2, 0, 0, 0, 3, 0, 0, 0, 1, 0, 0 };

         var clues_unsolvable_4x4 = new int[] { 0, 0, 1, 2, 0, 2, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0 };

         var expected1_4x4 = new[]{new []{ 2, 1, 4, 3 },
                                   new []{ 3, 4, 1, 2 },
                                   new []{ 4, 2, 3, 1 },
                                   new []{ 1, 3, 2, 4 }};

         Program p = new Program();

         var watch = System.Diagnostics.Stopwatch.StartNew();

         p.SolvePuzzle(clues_8x8_hard);

         int[][] solution1_8x8 = p.SolvePuzzle(clues1_8x8);
         int[][] solution2_8x8 = p.SolvePuzzle(clues2_8x8);

         int[][] solution1_7x7 = p.SolvePuzzle(clues1_7x7);
         int[][] solution2_7x7 = p.SolvePuzzle(clues2_7x7);

         int[][] solution1_6x6 = p.SolvePuzzle(clues1_6x6);
         int[][] solution2_6x6 = p.SolvePuzzle(clues2_6x6);
         int[][] solution3_6x6 = p.SolvePuzzle(clues3_6x6);

         int[][] solution1_4x4 = p.SolvePuzzle(clues1_4x4);

         int[][] solution_unsolvable = p.SolvePuzzle(clues_unsolvable_4x4);

         watch.Stop();

         Console.WriteLine("Execution time: " + watch.ElapsedMilliseconds + "\n");

         CollectionAssert.AreEqual(expected1_7x7, solution1_7x7);
         CollectionAssert.AreEqual(expected2_7x7, solution2_7x7);
         CollectionAssert.AreEqual(expected1_6x6, solution1_6x6);
         CollectionAssert.AreEqual(expected2_6x6, solution2_6x6);
         CollectionAssert.AreEqual(expected3_6x6, solution3_6x6);
         CollectionAssert.AreEqual(expected1_4x4, solution1_4x4);

         Console.ReadKey();
      }
   }
}