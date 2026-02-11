using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DwarfFrontierComplete
{
    // ==================== DÜŞMAN SINIFI ====================
    public class AdvancedEntity
    {
        public int x { get; set; }
        public int y { get; set; }
        public char type { get; set; }
        public int hp { get; set; }
        public int maxHP { get; set; }
        public string name { get; set; }
        public int damage { get; set; }
        public int level { get; set; }
        public int xpValue { get; set; }
        public string[] attacks { get; set; }
        public ConsoleColor color { get; set; }
        public List<StatusEffect> effects { get; set; }
        
        public AdvancedEntity()
        {
            effects = new List<StatusEffect>();
        }
    }

    // ==================== STATÜ ETKİSİ ====================
    public class StatusEffect
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public Action<AdvancedEntity> OnTurn { get; set; }
        public Action<AdvancedEntity> OnEnd { get; set; }
    }

    // ==================== YETENEK SINIFI ====================
    public class Skill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int StaminaCost { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public Action<AdvancedEntity> Effect { get; set; }
    }

    // ==================== ANA PROGRAM ====================
    class Program
    {
        // --- DÜNYA VE STATLAR ---
        static int worldSize = 500;
        static char[,] worldMap = new char[worldSize, worldSize];
        static int screenW = 60, screenH = 20;

        static int px = 250, py = 250;
        static double health = 100, maxHealth = 100, hunger = 100;
        static int stone = 50, wood = 50, coal = 0, iron = 0, diamond = 0, arrows = 5;
        static int level = 1, xp = 0;

        // --- SAVAŞ STATLARI ---
        static int strength = 10;
        static int dexterity = 10;
        static int stamina = 100;
        static int maxStamina = 100;
        static int comboCounter = 0;
        static string combatLog = "";
        
        // --- SAVAŞ DURUMU ---
        static bool inCombat = false;
        static AdvancedEntity currentEnemy = null;
        
        // --- ENVANTER VE EKİPMAN ---
        static bool hasBow = false;
        static bool hasShield = false;
        static int armorLevel = 0;
        static int pickaxeLevel = 1;

        // --- DÜNYA VE ZAMAN ---
        static int worldTime = 800;
        static bool buildMode = false;
        static int bx, by;
        static List<AdvancedEntity> entities = new List<AdvancedEntity>();
        static string status = "E:Envanter C:Craft T:Tüccar B:İnşa D:Demirci W:Büyücü Q:Kaydet";
        
        // --- YETENEKLER ---
        static Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
        static List<string> unlockedSkills = new List<string>();
        
        // --- SAVAŞ İSTATİSTİKLERİ ---
        static Dictionary<string, int> combatStats = new Dictionary<string, int>();
        
        // --- HAVA DURUMU ---
        static string weather = "Acik";
        static int weatherTimer = 0;
        
        // --- SAYFA YÖNETİMİ ---
        static Stack<string> pageStack = new Stack<string>();
        static bool inMenu = false;
        
        // --- ZAMANLAYICILAR ---
        static int autoSaveTimer = 0;
        static DateTime gameStartTime;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            
            InitializeSkills();
            InitializeCombatStats();
            
            OpenPage("mainmenu");
            ShowMainMenu();
            
            gameStartTime = DateTime.Now;
            
            while (health > 0)
            {
                if (inMenu)
                {
                    System.Threading.Thread.Sleep(100);
                    continue;
                }
                
                UpdateTime();
                UpdateWeather();
                AutoSave();
                
                if (inCombat && currentEnemy != null)
                {
                    DrawCombatUI();
                    HandleCombatInput();
                }
                else
                {
                    Draw();
                    HandleInput();
                }
                
                UpdateLogic();
                System.Threading.Thread.Sleep(50);
            }
            
            GameOver();
        }

        // ==================== SAYFA YÖNETİMİ ====================
        static void OpenPage(string pageName)
        {
            pageStack.Push(pageName);
            inMenu = true;
            Console.Clear();
        }

        static void ClosePage()
        {
            if (pageStack.Count > 0)
            {
                pageStack.Pop();
            }
            
            if (pageStack.Count == 0)
            {
                inMenu = false;
                Console.Clear();
            }
            else
            {
                string previousPage = pageStack.Peek();
                switch (previousPage)
                {
                    case "inventory": ShowInventory(); break;
                    case "craft": CraftMenu(); break;
                    case "trade": Trade(); break;
                    case "save": SaveMenu(); break;
                    case "mainmenu": ShowMainMenu(); break;
                    case "blacksmith": Blacksmith(); break;
                    case "wizard": Wizard(); break;
                }
            }
        }

        static void ReturnToGame()
        {
            pageStack.Clear();
            inMenu = false;
            Console.Clear();
        }

        // ==================== YETENEK SİSTEMİ ====================
        static void InitializeSkills()
        {
            skills["vurus"] = new Skill
            {
                Name = "Cift Vurus",
                Description = "Arka arkaya 2 saldiri yap",
                StaminaCost = 20,
                Cooldown = 3,
                Effect = (enemy) =>
                {
                    int damage1 = (strength + level * 2) * (comboCounter + 1);
                    int damage2 = (strength + level * 2) / 2;
                    enemy.hp -= damage1 + damage2;
                    combatLog = $"CIFT VURUS! {damage1}+{damage2} hasar!";
                    comboCounter++;
                    combatStats["toplam_hasar"] += damage1 + damage2;
                }
            };

            skills["savurma"] = new Skill
            {
                Name = "Savurma",
                Description = "Tum dusmanlara hasar ver",
                StaminaCost = 35,
                Cooldown = 5,
                Effect = (enemy) =>
                {
                    int totalDamage = 0;
                    foreach (var e in entities.FindAll(e => e.type == 'E' && 
                             Math.Abs(e.x - px) <= 2 && Math.Abs(e.y - py) <= 2))
                    {
                        int damage = (strength + level) / 2;
                        e.hp -= damage;
                        totalDamage += damage;
                    }
                    combatLog = $"SAVURMA! {totalDamage} toplam hasar!";
                    combatStats["toplam_hasar"] += totalDamage;
                }
            };

            skills["nissan"] = new Skill
            {
                Name = "Keskin Nisan",
                Description = "2x ok hasari, garanti isabet",
                StaminaCost = 25,
                Cooldown = 4,
                Effect = (enemy) =>
                {
                    if (hasBow && arrows > 0)
                    {
                        int damage = (dexterity * 2 + level * 3) * 2;
                        enemy.hp -= damage;
                        arrows--;
                        combatLog = $"KESKIN NISAN! {damage} hasar!";
                        combatStats["toplam_hasar"] += damage;
                    }
                }
            };

            skills["ok_yagmuru"] = new Skill
            {
                Name = "Ok Yagmuru",
                Description = "5 ok firlat, rastgele dusmanlara",
                StaminaCost = 40,
                Cooldown = 6,
                Effect = (enemy) =>
                {
                    if (hasBow && arrows >= 5)
                    {
                        arrows -= 5;
                        int hits = 0;
                        int totalDamage = 0;
                        foreach (var e in entities.FindAll(e => e.type == 'E'))
                        {
                            if (new Random().Next(100) < 60)
                            {
                                int damage = dexterity + level;
                                e.hp -= damage;
                                totalDamage += damage;
                                hits++;
                            }
                        }
                        combatLog = $"OK YAGMURU! {hits} isabet, {totalDamage} hasar!";
                        combatStats["toplam_hasar"] += totalDamage;
                    }
                }
            };

            skills["kalkan"] = new Skill
            {
                Name = "Kalkan Duvasi",
                Description = "2 tur boyunca %50 az hasar",
                StaminaCost = 30,
                Cooldown = 6,
                Effect = (enemy) =>
                {
                    if (hasShield)
                    {
                        combatLog = "KALKAN DUVASI! 2 tur boyunca hasar yariya dustu!";
                    }
                }
            };

            skills["kan_okcu"] = new Skill
            {
                Name = "Kan Oku",
                Description = "50 cana 100 hasar",
                StaminaCost = 0,
                Cooldown = 8,
                Effect = (enemy) =>
                {
                    if (health > 50)
                    {
                        health -= 50;
                        enemy.hp -= 100;
                        combatLog = "KAN OKU! 100 hasar, 50 can kaybettin!";
                        combatStats["toplam_hasar"] += 100;
                    }
                }
            };

            skills["ates_topu"] = new Skill
            {
                Name = "Ates Topu",
                Description = "3x hasar, yanma etkisi",
                StaminaCost = 40,
                Cooldown = 5,
                Effect = (enemy) =>
                {
                    int damage = (strength + level * 3) * 3;
                    enemy.hp -= damage;
                    combatLog = $"ATES TOPU! {damage} hasar!";
                    combatStats["toplam_hasar"] += damage;
                }
            };

            unlockedSkills.Add("vurus");
        }

        static void InitializeCombatStats()
        {
            combatStats["canavar_oldurme"] = 0;
            combatStats["toplam_hasar"] = 0;
            combatStats["kritik_vurus"] = 0;
            combatStats["kombolar"] = 0;
        }

        // ==================== DÜNYA OLUŞTURMA ====================
        static void GenerateWorld()
        {
            Random rng = new Random();
            for (int y = 0; y < worldSize; y++)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    int r = rng.Next(1000);
                    if (r < 25) worldMap[x, y] = 'T';
                    else if (r < 40) worldMap[x, y] = 'O';
                    else if (r < 55) worldMap[x, y] = 'K';
                    else if (r < 65) worldMap[x, y] = 'I';
                    else if (r < 68) worldMap[x, y] = 'S';
                    else if (r < 110) worldMap[x, y] = '#';
                    else if (r < 112) worldMap[x, y] = '>';
                    else worldMap[x, y] = '.';
                }
            }
            GenerateVillage(260, 260);
            CreateEnemies();
        }

        // ==================== GELİŞMİŞ KÖY OLUŞTURMA ====================
        static void GenerateVillage(int vx, int vy)
        {
            Random rand = new Random();
            
            // KÖY MEYDANI (10x10 alan)
            for (int i = -5; i < 5; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    int x = vx + i;
                    int y = vy + j;
                    if (x >= 0 && x < worldSize && y >= 0 && y < worldSize)
                    {
                        if (rand.Next(100) < 70)
                            worldMap[x, y] = '_';
                        else
                            worldMap[x, y] = ',';
                    }
                }
            }
            
            // KÖY MERKEZİ - ÇEŞME
            worldMap[vx, vy] = '^';
            
            // TÜCCAR
            entities.Add(new AdvancedEntity 
            { 
                x = vx + 2, y = vy + 1, 
                type = 'D', hp = 100, maxHP = 100,
                name = "Tuccar", 
                damage = 0, level = 1, xpValue = 0,
                color = ConsoleColor.Yellow 
            });
            
            // KÖY MUHAFIZI (4 tane)
            int[] guardPosX = { -3, 3, -2, 4 };
            int[] guardPosY = { -2, 2, 3, -3 };
            
            for (int g = 0; g < 4; g++)
            {
                entities.Add(new AdvancedEntity 
                { 
                    x = vx + guardPosX[g], y = vy + guardPosY[g], 
                    type = 'M', hp = 300, maxHP = 300,
                    name = "Koy Muhafizi",
                    damage = 25, level = 5, xpValue = 0,
                    color = ConsoleColor.White 
                });
            }
            
            // DEMİRCİ
            entities.Add(new AdvancedEntity 
            { 
                x = vx - 2, y = vy - 2, 
                type = 'B', hp = 80, maxHP = 80,
                name = "Demirci", 
                damage = 0, level = 1, xpValue = 0,
                color = ConsoleColor.DarkYellow 
            });
            worldMap[vx - 2, vy - 2] = 'B';
            
            // BÜYÜCÜ
            entities.Add(new AdvancedEntity 
            { 
                x = vx + 3, y = vy - 3, 
                type = 'W', hp = 60, maxHP = 60,
                name = "Buyucu", 
                damage = 0, level = 1, xpValue = 0,
                color = ConsoleColor.Magenta 
            });
            worldMap[vx + 3, vy - 3] = 'W';
            
            // KÖYLÜLER (10 tane)
            string[] koyluIsimleri = { "Ciftci", "Oduncu", "Avci", "Balikci", "Firinci", "Marangoz", "Terzi", "Sifaci" };
            
            for (int k = 0; k < 10; k++)
            {
                int kx, ky;
                do
                {
                    kx = vx + rand.Next(-4, 5);
                    ky = vy + rand.Next(-4, 5);
                } while (worldMap[kx, ky] != '_' && worldMap[kx, ky] != ',');
                
                entities.Add(new AdvancedEntity 
                { 
                    x = kx, y = ky, 
                    type = 'V', 
                    hp = 50, maxHP = 50,
                    name = koyluIsimleri[rand.Next(koyluIsimleri.Length)], 
                    damage = 0, level = 1, xpValue = 0,
                    color = ConsoleColor.Green 
                });
            }
            
            // KÖY EVLERİ (8 tane)
            for (int e = 0; e < 8; e++)
            {
                int ex, ey;
                do
                {
                    ex = vx + rand.Next(-5, 6);
                    ey = vy + rand.Next(-5, 6);
                } while (Math.Abs(ex - vx) < 2 && Math.Abs(ey - vy) < 2);
                
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int x = ex + i;
                        int y = ey + j;
                        if (x >= 0 && x < worldSize && y >= 0 && y < worldSize)
                        {
                            if (i == 0 && j == 0)
                                worldMap[x, y] = 'H';
                            else
                                worldMap[x, y] = 'W';
                        }
                    }
                }
            }
            
            // MEYVE BAHÇESİ
            for (int m = 0; m < 6; m++)
            {
                int mx = vx + rand.Next(-5, 6);
                int my = vy + rand.Next(-5, 6);
                worldMap[mx, my] = 'F';
            }
            
            // AHIR
            if (vx + 5 < worldSize && vy - 1 >= 0) worldMap[vx + 5, vy - 1] = 'C';
            if (vx + 5 < worldSize && vy < worldSize) worldMap[vx + 5, vy] = 'C';
            if (vx + 5 < worldSize && vy + 1 < worldSize) worldMap[vx + 5, vy + 1] = 'S';
            
            // KUYU
            if (vx - 4 >= 0 && vy + 3 < worldSize) worldMap[vx - 4, vy + 3] = '=';
            
            // MEZARLIK
            for (int z = 0; z < 4; z++)
            {
                if (vx - 5 + z >= 0 && vy + 4 < worldSize)
                    worldMap[vx - 5 + z, vy + 4] = '+';
            }
            
            status = "Buyuk bir koy kesfettin!";
        }

        static void CreateEnemies()
        {
            entities.Add(new AdvancedEntity
            {
                x = 300, y = 300,
                type = 'G',
                name = "Goblin Savasci",
                hp = 45, maxHP = 45,
                damage = 8,
                level = 2,
                xpValue = 40,
                color = ConsoleColor.Green,
                attacks = new[] { "vurus", "kacis" }
            });
            
            entities.Add(new AdvancedEntity
            {
                x = 310, y = 290,
                type = 'T',
                name = "Orman Trollu",
                hp = 120, maxHP = 120,
                damage = 18,
                level = 5,
                xpValue = 120,
                color = ConsoleColor.DarkRed,
                attacks = new[] { "savurma", "vurus", "korkutma" }
            });
            
            entities.Add(new AdvancedEntity
            {
                x = 280, y = 315,
                type = 'Z',
                name = "Zehirli Orumcek",
                hp = 35, maxHP = 35,
                damage = 6,
                level = 3,
                xpValue = 50,
                color = ConsoleColor.DarkYellow,
                attacks = new[] { "isirik", "zehir" }
            });
            
            entities.Add(new AdvancedEntity
            {
                x = 290, y = 270,
                type = 'H',
                name = "Hayalet",
                hp = 80, maxHP = 80,
                damage = 12,
                level = 4,
                xpValue = 80,
                color = ConsoleColor.Cyan,
                attacks = new[] { "vurus", "korkutma" }
            });
        }

        // ==================== ZAMAN VE HAVA ====================
        static void UpdateTime()
        {
            worldTime += 2;
            if (worldTime >= 2400) worldTime = 0;
            
            hunger -= 0.02;
            if (hunger < 0) { hunger = 0; health -= 0.1; }
            
            if (stamina < maxStamina && !inCombat)
                stamina += 1;
        }

        static void UpdateWeather()
        {
            weatherTimer++;
            if (weatherTimer > 500)
            {
                string[] weathers = { "Acik", "Bulutlu", "Yagmurlu", "Firtinali" };
                weather = weathers[new Random().Next(weathers.Length)];
                weatherTimer = 0;
            }
        }

        // ==================== ÇİZİM FONKSİYONLARI ====================
        static void Draw()
        {
            if (inCombat) return;
            
            Console.SetCursorPosition(0, 0);
            bool isNight = (worldTime > 1800 || worldTime < 600);
            int viewRange = isNight ? 8 : 35;

            int startX = Math.Clamp(px - screenW / 2, 0, worldSize - screenW);
            int startY = Math.Clamp(py - screenH / 2, 0, worldSize - screenH);

            for (int y = startY; y < startY + screenH; y++)
            {
                for (int x = startX; x < startX + screenW; x++)
                {
                    double dist = Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2));
                    if (dist > viewRange && worldMap[x, y] != 'i') 
                    { 
                        Console.Write(' '); 
                        continue; 
                    }

                    AdvancedEntity ent = entities.Find(e => e.x == x && e.y == y);
                    
                    if (x == px && y == py) 
                    { 
                        Console.ForegroundColor = ConsoleColor.Cyan; 
                        Console.Write('@'); 
                    }
                    else if (buildMode && x == bx && y == by) 
                    { 
                        Console.ForegroundColor = ConsoleColor.Magenta; 
                        Console.Write('X'); 
                    }
                    else if (ent != null)
                    {
                        Console.ForegroundColor = ent.color;
                        Console.Write(ent.type);
                    }
                    else 
                    { 
                        SetColor(worldMap[x, y]); 
                        Console.Write(worldMap[x, y]); 
                    }
                }
                Console.WriteLine();
            }
            
            Console.ResetColor();
            Console.WriteLine($"LVL:{level} | CAN:{(int)health} | ACLIK:{(int)hunger} | {weather} | SAAT:{worldTime / 100:D2}:00");
            Console.WriteLine($"Dayaniklilik: {stamina}/{maxStamina} | Guc:{strength} | Cevik:{dexterity}");
            Console.WriteLine($"{status.PadRight(screenW)}");
        }

        static void DrawCombatUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=================== SAVAS MODU ===================");
            Console.ResetColor();
            
            if (currentEnemy == null) return;
            
            Console.WriteLine($"\n{' ',10}----------------------------------------");
            Console.ForegroundColor = currentEnemy.color;
            Console.WriteLine($"{' ',15}>> {currentEnemy.name} (Seviye {currentEnemy.level}) <<");
            Console.ResetColor();
            DrawHealthBar(currentEnemy.hp, currentEnemy.maxHP, 50, ConsoleColor.Red);
            
            Console.WriteLine($"\n{' ',10}----------------------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{' ',15}>> Seviye {level} Cuce Savasci <<");
            Console.ResetColor();
            DrawHealthBar((int)health, (int)maxHealth, 50, ConsoleColor.Green);
            
            Console.Write($"\n{' ',10}Dayaniklilik: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            int staminaBars = stamina / 2;
            Console.Write(new string('█', staminaBars));
            Console.Write(new string('░', (maxStamina / 2) - staminaBars));
            Console.ResetColor();
            Console.WriteLine($" {stamina}/{maxStamina}");
            
            if (comboCounter > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n{' ',15}>>> KOMBO x{comboCounter}! <<<");
                Console.ResetColor();
            }
            
            Console.WriteLine($"\n{' ',10}============= YETENEKLER =============");
            int slot = 1;
            foreach (var skillName in unlockedSkills)
            {
                if (skills.TryGetValue(skillName, out Skill s))
                {
                    Console.ForegroundColor = s.CurrentCooldown > 0 ? ConsoleColor.DarkGray : ConsoleColor.White;
                    Console.Write($"{' ',12}[{slot}] {s.Name,-15} ");
                    Console.Write($"| {s.StaminaCost} SP ");
                    if (s.CurrentCooldown > 0)
                        Console.Write($"| Soguma: {s.CurrentCooldown}");
                    else
                        Console.Write($"| {s.Description}");
                    Console.WriteLine();
                    Console.ResetColor();
                }
                slot++;
            }
            
            Console.WriteLine($"\n{' ',10}============= KOMUTLAR =============");
            Console.WriteLine($"{' ',12}[A] Normal Saldiri  [F] Ok At ({arrows})");
            Console.WriteLine($"{' ',12}[R] Kac            [ESC] Cikis");
            
            Console.WriteLine($"\n{' ',10}========================================");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{' ',12}{combatLog}");
            Console.ResetColor();
        }

        static void DrawHealthBar(int current, int max, int width, ConsoleColor color)
        {
            Console.Write($"{' ',12}[");
            int filled = (int)((double)current / max * width);
            Console.ForegroundColor = color;
            Console.Write(new string('█', filled));
            Console.ResetColor();
            Console.Write(new string('░', width - filled));
            Console.Write($"] {current}/{max}\n");
        }

        static void SetColor(char c)
        {
            switch (c)
            {
                // DOĞAL KAYNAKLAR
                case 'T': Console.ForegroundColor = ConsoleColor.Green; break;
                case 'O': Console.ForegroundColor = ConsoleColor.Gray; break;
                case 'K': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case 'I': Console.ForegroundColor = ConsoleColor.Blue; break;
                case 'S': Console.ForegroundColor = ConsoleColor.Cyan; break;
                case ',': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case 'i': Console.ForegroundColor = ConsoleColor.Yellow; break;
                case '#': Console.ForegroundColor = ConsoleColor.Gray; break;
                case '=': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case '/': Console.ForegroundColor = ConsoleColor.Red; break;
                case '>': Console.ForegroundColor = ConsoleColor.Magenta; break;
                
                // KÖY ELEMANLARI
                case '_': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case '^': Console.ForegroundColor = ConsoleColor.Cyan; break;
                case 'B': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case 'W': Console.ForegroundColor = ConsoleColor.Magenta; break;
                case 'V': Console.ForegroundColor = ConsoleColor.Green; break;
                case 'M': Console.ForegroundColor = ConsoleColor.White; break;
                case 'H': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case 'F': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case 'C': Console.ForegroundColor = ConsoleColor.White; break;
                case 'G': Console.ForegroundColor = ConsoleColor.White; break;
                case '+': Console.ForegroundColor = ConsoleColor.Red; break;
                
                default: Console.ForegroundColor = ConsoleColor.DarkGray; break;
            }
        }

        // ==================== GİRDİ İŞLEME ====================
        static void HandleInput()
        {
            if (!Console.KeyAvailable || inMenu) return;
            
            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.Q) { SaveGame(); Environment.Exit(0); }
            if (key == ConsoleKey.E) { ShowInventory(); return; }
            if (key == ConsoleKey.B) { buildMode = !buildMode; bx = px; by = py; return; }
            if (key == ConsoleKey.C) { CraftMenu(); return; }
            if (key == ConsoleKey.T) { Trade(); return; }
            if (key == ConsoleKey.F5) { SaveGame(); return; }
            if (key == ConsoleKey.F9) { LoadGame(); return; }
            
            if (key == ConsoleKey.D) 
            { 
                AdvancedEntity smith = entities.Find(e => e.type == 'B' && 
                    Math.Abs(e.x - px) <= 1 && Math.Abs(e.y - py) <= 1);
                if (smith != null)
                {
                    Blacksmith();
                    return;
                }
            }
            
            if (key == ConsoleKey.W) 
            { 
                AdvancedEntity wizard = entities.Find(e => e.type == 'W' && 
                    Math.Abs(e.x - px) <= 1 && Math.Abs(e.y - py) <= 1);
                if (wizard != null)
                {
                    Wizard();
                    return;
                }
            }

            if (buildMode)
            {
                if (key == ConsoleKey.UpArrow) by--; 
                if (key == ConsoleKey.DownArrow) by++;
                if (key == ConsoleKey.LeftArrow) bx--; 
                if (key == ConsoleKey.RightArrow) bx++;
                
                bx = Math.Clamp(bx, 0, worldSize - 1);
                by = Math.Clamp(by, 0, worldSize - 1);
                
                if (key == ConsoleKey.D1 && stone >= 5) { worldMap[bx, by] = '#'; stone -= 5; }
                if (key == ConsoleKey.D2 && wood >= 5) { worldMap[bx, by] = '='; wood -= 5; }
                if (key == ConsoleKey.D3 && wood >= 10) { worldMap[bx, by] = '/'; wood -= 10; }
                if (key == ConsoleKey.D4 && coal >= 1) { worldMap[bx, by] = 'i'; coal -= 1; }
                if (key == ConsoleKey.D5 && wood >= 2) { worldMap[bx, by] = ','; wood -= 2; }
            }
            else
            {
                int nx = px, ny = py;
                if (key == ConsoleKey.UpArrow) ny--; 
                else if (key == ConsoleKey.DownArrow) ny++;
                else if (key == ConsoleKey.LeftArrow) nx--; 
                else if (key == ConsoleKey.RightArrow) nx++;
                else if (key == ConsoleKey.Spacebar) Attack(true);
                else if (key == ConsoleKey.F) Attack(false);

                if (nx >= 0 && nx < worldSize && ny >= 0 && ny < worldSize)
                {
                    char t = worldMap[nx, ny];
                    if (t == 'T' || t == 'O' || t == 'K' || t == 'I' || t == 'S' || t == '#')
                    {
                        Mine(nx, ny);
                        worldMap[nx, ny] = '.';
                        xp += 10 * pickaxeLevel;
                        CheckLvl();
                    }
                    else if (t == '>')
                    {
                        px = 100; py = 100;
                        status = "Zindana Girdin! Dikkatli ol!";
                    }
                    else 
                    { 
                        px = nx; 
                        py = ny; 
                    }
                }
            }
        }

        static void HandleCombatInput()
        {
            if (!inCombat || currentEnemy == null) return;
            
            var key = Console.ReadKey(true).Key;
            
            switch (key)
            {
                case ConsoleKey.D1: CombatAction(0); break;
                case ConsoleKey.D2: CombatAction(1); break;
                case ConsoleKey.D3: CombatAction(2); break;
                case ConsoleKey.D4: CombatAction(3); break;
                
                case ConsoleKey.A:
                    int baseDamage = strength + level * 2;
                    Random rand = new Random();
                    if (rand.Next(100) < (10 + level))
                    {
                        baseDamage *= 2;
                        combatStats["kritik_vurus"]++;
                        combatLog = "KRITIK VURUS!";
                    }
                    currentEnemy.hp -= baseDamage;
                    stamina = Math.Min(maxStamina, stamina + 10);
                    combatLog += $" {baseDamage} hasar verdin!";
                    combatStats["toplam_hasar"] += baseDamage;
                    
                    if (currentEnemy.hp <= 0)
                    {
                        EndCombat(true);
                        return;
                    }
                    EnemyAttack();
                    break;
                    
                case ConsoleKey.F:
                    if (hasBow && arrows > 0)
                    {
                        int bowDamage = dexterity + level * 2;
                        currentEnemy.hp -= bowDamage;
                        arrows--;
                        stamina += 5;
                        combatLog = $"Ok! {bowDamage} hasar!";
                        combatStats["toplam_hasar"] += bowDamage;
                        
                        if (currentEnemy.hp <= 0)
                        {
                            EndCombat(true);
                            return;
                        }
                        EnemyAttack();
                    }
                    else
                    {
                        combatLog = "Yayin yok veya okun bitti!";
                    }
                    break;
                    
                case ConsoleKey.R:
                    if (new Random().Next(100) < 50 + dexterity)
                    {
                        combatLog = "Savastan kactin!";
                        inCombat = false;
                        currentEnemy = null;
                    }
                    else
                    {
                        combatLog = "Kacamadin!";
                        EnemyAttack();
                    }
                    break;
                    
                case ConsoleKey.Escape:
                    inCombat = false;
                    currentEnemy = null;
                    break;
            }
            
            foreach (var skill in skills.Values)
                if (skill.CurrentCooldown > 0)
                    skill.CurrentCooldown--;
        }

        static void CombatAction(int skillIndex)
        {
            if (!inCombat || currentEnemy == null) return;
            
            if (skillIndex >= 0 && skillIndex < unlockedSkills.Count)
            {
                string skillName = unlockedSkills[skillIndex];
                if (skills.TryGetValue(skillName, out Skill skill))
                {
                    if (stamina < skill.StaminaCost)
                    {
                        combatLog = "Yetersiz dayaniklilik!";
                        return;
                    }
                    
                    if (skill.CurrentCooldown > 0)
                    {
                        combatLog = $"{skill.Name} beklemede! ({skill.CurrentCooldown} tur)";
                        return;
                    }
                    
                    stamina -= skill.StaminaCost;
                    skill.Effect(currentEnemy);
                    skill.CurrentCooldown = skill.Cooldown;
                    
                    if (currentEnemy.hp <= 0)
                    {
                        EndCombat(true);
                        return;
                    }
                    
                    EnemyAttack();
                }
            }
        }

        static void EnemyAttack()
        {
            if (currentEnemy == null) return;
            
            Random rand = new Random();
            
            int dodgeChance = dexterity / 2;
            if (rand.Next(100) < dodgeChance)
            {
                combatLog = "Dusman saldirisindan kactin!";
                return;
            }
            
            bool enemyCrit = rand.Next(100) < 10;
            int damage = currentEnemy.damage;
            
            if (enemyCrit)
            {
                damage *= 2;
                combatLog = "DUSMAN KRITIK VURUS!";
            }
            
            if (hasShield && rand.Next(100) < 40)
            {
                damage /= 2;
                combatLog += " Kalkanla bloklandi!";
            }
            
            damage = (int)(damage * (1 - (armorLevel * 0.1)));
            if (damage < 1) damage = 1;
            
            health -= damage;
            combatLog += $" Dusman {damage} hasar verdi!";
            
            if (health <= 0)
            {
                health = 0;
                EndCombat(false);
            }
        }

        static void Mine(int x, int y)
        {
            char tile = worldMap[x, y];
            switch(tile)
            {
                case 'T': wood += 5 * pickaxeLevel; break;
                case 'I': iron += 3 * pickaxeLevel; break;
                case 'S': diamond += 1 * pickaxeLevel; break;
                case 'K': coal += 5 * pickaxeLevel; break;
                case 'O': stone += 2 * pickaxeLevel; break;
                case '#': stone += 2 * pickaxeLevel; break;
            }
            status = $"Madencilik: {tile} kazildi!";
        }

        static void Attack(bool close)
        {
            if (inCombat) return;
            
            AdvancedEntity target = entities.Find(e => e.type == 'E' && 
                Math.Abs(e.x - px) <= (close ? 1 : 10) && 
                Math.Abs(e.y - py) <= (close ? 1 : 10));
            
            if (target != null)
            {
                EnterCombat(target);
            }
        }

        static void EnterCombat(AdvancedEntity enemy)
        {
            inCombat = true;
            currentEnemy = enemy;
            comboCounter = 0;
            combatLog = $"SAVAS BASLADI! Seviye {enemy.level} {enemy.name} ile karsilastin!";
            
            foreach (var skill in skills.Values)
                skill.CurrentCooldown = 0;
        }

        static void EndCombat(bool victory)
        {
            if (victory)
            {
                int xpGain = currentEnemy.xpValue + comboCounter * 5;
                xp += xpGain;
                combatStats["canavar_oldurme"]++;
                combatStats["kombolar"] += comboCounter;
                
                CheckLvl();
                
                Random rand = new Random();
                if (rand.Next(100) < 40)
                {
                    string[] loots = { "Tas", "Demir", "Elmas", "Ok", "Komur" };
                    string loot = loots[rand.Next(loots.Length)];
                    int amount = rand.Next(1, 6) * currentEnemy.level;
                    
                    switch(loot)
                    {
                        case "Tas": stone += amount; break;
                        case "Demir": iron += amount; break;
                        case "Elmas": diamond += amount; break;
                        case "Ok": arrows += amount; break;
                        case "Komur": coal += amount; break;
                    }
                    
                    combatLog = $"Zafer! +{xpGain} XP, {amount} {loot} kazandin!";
                }
                else
                {
                    combatLog = $"Zafer! +{xpGain} XP!";
                }
                
                entities.Remove(currentEnemy);
                
                if (level >= 3 && !unlockedSkills.Contains("kalkan"))
                    unlockedSkills.Add("kalkan");
                if (level >= 5 && !unlockedSkills.Contains("nissan"))
                    unlockedSkills.Add("nissan");
                if (level >= 8 && !unlockedSkills.Contains("ok_yagmuru"))
                    unlockedSkills.Add("ok_yagmuru");
                if (level >= 10 && !unlockedSkills.Contains("kan_okcu"))
                    unlockedSkills.Add("kan_okcu");
                if (level >= 12 && !unlockedSkills.Contains("savurma"))
                    unlockedSkills.Add("savurma");
            }
            else
            {
                combatLog = "Yenildin!";
            }
            
            inCombat = false;
            currentEnemy = null;
        }

        static void CheckLvl() 
        { 
            if (xp >= level * 100) 
            { 
                level++; 
                xp = 0; 
                maxHealth += 20; 
                health = maxHealth;
                strength += 2;
                dexterity += 2;
                maxStamina += 10;
                stamina = maxStamina;
                status = "SEVIYE ATLADIN!";
            } 
        }

        // ==================== OYUN MANTIĞI ====================
        static void UpdateLogic()
        {
            if (worldMap[px, py] == ',') 
            { 
                hunger = Math.Min(100, hunger + 0.1); 
                health = Math.Min(maxHealth, health + 0.1);
            }
            
            foreach (var ent in entities)
            {
                if (ent.type == 'E' && ent != currentEnemy)
                {
                    if (Math.Abs(ent.x - px) < 20 && Math.Abs(ent.y - py) < 20)
                    {
                        if (ent.x < px) ent.x++; 
                        else if (ent.x > px) ent.x--;
                        if (ent.y < py) ent.y++; 
                        else if (ent.y > py) ent.y--;
                    }
                    
                    if (ent.x == px && ent.y == py && !inCombat)
                    {
                        EnterCombat(ent);
                    }
                }
            }
            
            if (!inCombat && health < maxHealth)
                health += 0.05;
        }

        // ==================== MENÜ FONKSİYONLARI ====================
        static void ShowMainMenu()
        {
            while (inMenu && pageStack.Peek() == "mainmenu")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@"
    ╔══════════════════════════════════════════════════════════╗
    ║                                                          ║
    ║              C U C E   F R O N T I E R                  ║
    ║                                                          ║
    ╠══════════════════════════════════════════════════════════╣
    ║                                                          ║
    ║              [1] YENI MACERA                            ║
    ║              [2] KAYIT YUKLE                            ║
    ║              [3] KAYIT YONETICISI                       ║
    ║              [4] CIKIS                                  ║
    ║                                                          ║
    ╚══════════════════════════════════════════════════════════╝
                ");
                Console.ResetColor();
                
                var key = Console.ReadKey(true).Key;
                
                switch (key)
                {
                    case ConsoleKey.D1:
                        ClosePage();
                        ResetGame();
                        GenerateWorld();
                        break;
                        
                    case ConsoleKey.D2:
                        ClosePage();
                        LoadGame();
                        break;
                        
                    case ConsoleKey.D3:
                        ClosePage();
                        SaveMenu();
                        break;
                        
                    case ConsoleKey.D4:
                        Environment.Exit(0);
                        break;
                }
            }
        }

        static void ShowInventory()
        {
            OpenPage("inventory");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("          CUCE ENVANTERI                  ");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.ResetColor();
            
            Console.WriteLine($"\n [KAYNAKLAR] ");
            Console.WriteLine($" ┌─────────────────────────────────┐");
            Console.WriteLine($" │ Tas:    {stone,-8} Odun:   {wood,-8} │");
            Console.WriteLine($" │ Komur:  {coal,-8} Demir:  {iron,-8} │");
            Console.WriteLine($" │ Elmas:  {diamond,-8} Ok:     {arrows,-8} │");
            Console.WriteLine($" └─────────────────────────────────┘");
            
            Console.WriteLine($"\n [EKIPMANLAR] ");
            Console.WriteLine($" ┌─────────────────────────────────┐");
            Console.WriteLine($" │ Yay:     {(hasBow ? "VAR" : "YOK")}                    │");
            Console.WriteLine($" │ Kalkan:  {(hasShield ? "VAR" : "YOK")}                    │");
            Console.WriteLine($" │ Kazma:   Seviye {pickaxeLevel}                  │");
            Console.WriteLine($" │ Zirh:    Seviye {armorLevel}                  │");
            Console.WriteLine($" └─────────────────────────────────┘");
            
            Console.WriteLine($"\n [STATLAR] ");
            Console.WriteLine($" ┌─────────────────────────────────┐");
            Console.WriteLine($" │ Seviye: {level} (XP: {xp}/{level * 100})        │");
            Console.WriteLine($" │ Guc:    {strength,-8} Ceviklik: {dexterity,-8} │");
            Console.WriteLine($" │ Can:    {maxHealth,-8} Dayaniklilik: {maxStamina,-8} │");
            Console.WriteLine($" └─────────────────────────────────┘");
            
            Console.WriteLine($"\n [YETENEKLER] ");
            Console.WriteLine($" ┌─────────────────────────────────┐");
            foreach (var skill in unlockedSkills)
            {
                if (skills.TryGetValue(skill, out Skill s))
                    Console.WriteLine($" │ • {s.Name,-30} │");
            }
            Console.WriteLine($" └─────────────────────────────────┘");
            
            Console.WriteLine($"\n [BASARILAR] ");
            Console.WriteLine($" ┌─────────────────────────────────┐");
            Console.WriteLine($" │ Canavar Avciligi: {combatStats["canavar_oldurme"],-4}               │");
            Console.WriteLine($" │ Toplam Hasar:     {combatStats["toplam_hasar"],-4}               │");
            Console.WriteLine($" │ Kritik Vurus:     {combatStats["kritik_vurus"],-4}               │");
            Console.WriteLine($" │ Kombo Sayisi:     {combatStats["kombolar"],-4}               │");
            Console.WriteLine($" └─────────────────────────────────┘");
            
            Console.WriteLine("\n═══════════════════════════════════════════");
            Console.WriteLine(" [ESC] Geri Don  |  [M] Ana Menu");
            
            while (inMenu && pageStack.Peek() == "inventory")
            {
                var key = Console.ReadKey(true).Key;
                
                if (key == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else if (key == ConsoleKey.M)
                {
                    ReturnToGame();
                    OpenPage("mainmenu");
                    ShowMainMenu();
                }
            }
        }

        static void CraftMenu()
        {
            OpenPage("craft");
            
            while (inMenu && pageStack.Peek() == "craft")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("          ZANAAT (CRAFT) MENUSU            ");
                Console.WriteLine("═══════════════════════════════════════════");
                Console.ResetColor();
                
                Console.WriteLine($"\n [KAYNAKLARIN]");
                Console.WriteLine($" Tas: {stone} | Odun: {wood} | Demir: {iron} | Komur: {coal} | Elmas: {diamond}");
                Console.WriteLine("\n [TARIFLER]");
                Console.WriteLine(" ─────────────────────────────────────────");
                Console.WriteLine($" 1. Yay Uret          (30 Odun)    {(hasBow ? "VAR" : "YOK")}");
                Console.WriteLine($" 2. Ok Uret x10       (5 Tas + 2 Odun)   {arrows}");
                Console.WriteLine($" 3. Demir Kalkan      (30 Demir)   {(hasShield ? "VAR" : "YOK")}");
                Console.WriteLine($" 4. Demir Zirh        (50 Demir)   Seviye: {armorLevel}");
                Console.WriteLine($" 5. Kazma Yukselt     (20 Tas + 10 Demir) Seviye: {pickaxeLevel}");
                Console.WriteLine($" 6. Guc Iksiri        (5 Elmas + 10 Komur) Guc+2");
                Console.WriteLine($" 7. Ceviklik Iksiri   (5 Elmas + 10 Komur) Ceviklik+2");
                Console.WriteLine("\n [ESC] Geri Don  |  [M] Ana Menu");
                
                var k = Console.ReadKey(true).Key;
                
                if (k == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else if (k == ConsoleKey.M)
                {
                    ReturnToGame();
                    OpenPage("mainmenu");
                    ShowMainMenu();
                }
                else
                {
                    switch (k)
                    {
                        case ConsoleKey.D1:
                            if (wood >= 30 && !hasBow) { hasBow = true; wood -= 30; }
                            break;
                        case ConsoleKey.D2:
                            if (stone >= 5 && wood >= 2) { arrows += 10; stone -= 5; wood -= 2; }
                            break;
                        case ConsoleKey.D3:
                            if (iron >= 30 && !hasShield) { hasShield = true; iron -= 30; }
                            break;
                        case ConsoleKey.D4:
                            if (iron >= 50) { armorLevel++; iron -= 50; maxHealth += 25; health = maxHealth; }
                            break;
                        case ConsoleKey.D5:
                            if (stone >= 20 && iron >= 10) { pickaxeLevel++; stone -= 20; iron -= 10; }
                            break;
                        case ConsoleKey.D6:
                            if (diamond >= 5 && coal >= 10) { strength += 2; diamond -= 5; coal -= 10; }
                            break;
                        case ConsoleKey.D7:
                            if (diamond >= 5 && coal >= 10) { dexterity += 2; diamond -= 5; coal -= 10; }
                            break;
                    }
                }
            }
        }

        static void Trade()
        {
            AdvancedEntity trd = entities.Find(e => e.type == 'D' && 
                Math.Abs(e.x - px) <= 1 && Math.Abs(e.y - py) <= 1);
                
            if (trd == null)
            {
                status = "Yakinda tuccar yok!";
                return;
            }
            
            OpenPage("trade");
            
            while (inMenu && pageStack.Peek() == "trade")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("              T U C C A R                  ");
                Console.WriteLine("═══════════════════════════════════════════");
                Console.ResetColor();
                
                Console.WriteLine("\n [KAYNAKLARIN]");
                Console.WriteLine($" Tas: {stone} | Demir: {iron} | Elmas: {diamond}");
                Console.WriteLine("\n [FIYATLAR]");
                Console.WriteLine(" 1. 1 Elmas -> 200 Tas");
                Console.WriteLine(" 2. 50 Tas -> 20 Demir");
                Console.WriteLine(" 3. 20 Demir -> 1 Elmas");
                Console.WriteLine(" 4. 100 Tas -> Yay (Eger yoksa)");
                Console.WriteLine("\n [ESC] Geri Don  |  [M] Ana Menu");
                
                var k = Console.ReadKey(true).Key;
                
                if (k == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else if (k == ConsoleKey.M)
                {
                    ReturnToGame();
                    OpenPage("mainmenu");
                    ShowMainMenu();
                }
                else
                {
                    switch (k)
                    {
                        case ConsoleKey.D1:
                            if (diamond >= 1) { diamond--; stone += 200; }
                            break;
                        case ConsoleKey.D2:
                            if (stone >= 50) { stone -= 50; iron += 20; }
                            break;
                        case ConsoleKey.D3:
                            if (iron >= 20) { iron -= 20; diamond++; }
                            break;
                        case ConsoleKey.D4:
                            if (stone >= 100 && !hasBow) { stone -= 100; hasBow = true; }
                            break;
                    }
                }
            }
        }

        // ==================== DEMİRCİ MENÜSÜ ====================
        static void Blacksmith()
        {
            OpenPage("blacksmith");
            
            while (inMenu && pageStack.Peek() == "blacksmith")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("              D E M I R C I                ");
                Console.WriteLine("═══════════════════════════════════════════");
                Console.ResetColor();
                
                Console.WriteLine("\n [KAYNAKLARIN]");
                Console.WriteLine($" Tas: {stone} | Demir: {iron} | Elmas: {diamond} | Komur: {coal}");
                Console.WriteLine($"\n [GELISMIS URUNLER]");
                Console.WriteLine(" ─────────────────────────────────────────");
                Console.WriteLine($" 1.  Elmas Kilic     (20 Elmas + 30 Demir)  {(hasBow ? "VAR" : "YOK")}");
                Console.WriteLine($" 2.  Ejderha Kalkani (15 Elmas + 40 Demir)  {(hasShield ? "VAR" : "YOK")}");
                Console.WriteLine($" 3.  Mitril Zirh     (25 Elmas + 50 Demir)  Seviye: {armorLevel}");
                Console.WriteLine($" 4.  Kutsal Kazma    (10 Elmas + 30 Demir)  Seviye: {pickaxeLevel}");
                Console.WriteLine($" 5.  Guc Tilsimi     (5 Elmas + 20 Komur)   Guc+5");
                Console.WriteLine($" 6.  Ceviklik Tilsimi (5 Elmas + 20 Komur)   Ceviklik+5");
                Console.WriteLine($" 7.  Can Iksiri      (10 Elmas)            Max Can+50");
                Console.WriteLine($"\n [ESC] Geri Don");
                
                var k = Console.ReadKey(true).Key;
                
                if (k == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else
                {
                    switch (k)
                    {
                        case ConsoleKey.D1:
                            if (diamond >= 20 && iron >= 30) 
                            { 
                                diamond -= 20; iron -= 30; 
                                hasBow = true;
                                strength += 10;
                                status = "Elmas Kilic aldin! Guc +10!";
                            }
                            break;
                        case ConsoleKey.D2:
                            if (diamond >= 15 && iron >= 40 && !hasShield) 
                            { 
                                diamond -= 15; iron -= 40; 
                                hasShield = true;
                                status = "Ejderha Kalkani aldin! Blok sansi artti!";
                            }
                            break;
                        case ConsoleKey.D3:
                            if (diamond >= 25 && iron >= 50) 
                            { 
                                diamond -= 25; iron -= 50; 
                                armorLevel += 2;
                                maxHealth += 50;
                                health = maxHealth;
                                status = "Mitril Zirh! Can +50, Zirh +2!";
                            }
                            break;
                        case ConsoleKey.D4:
                            if (diamond >= 10 && iron >= 30) 
                            { 
                                diamond -= 10; iron -= 30; 
                                pickaxeLevel += 2;
                                status = "Kutsal Kazma! Madencilik seviyesi +2!";
                            }
                            break;
                        case ConsoleKey.D5:
                            if (diamond >= 5 && coal >= 20) 
                            { 
                                diamond -= 5; coal -= 20; 
                                strength += 5;
                                status = "Guc Tilsimi! Guc +5!";
                            }
                            break;
                        case ConsoleKey.D6:
                            if (diamond >= 5 && coal >= 20) 
                            { 
                                diamond -= 5; coal -= 20; 
                                dexterity += 5;
                                status = "Ceviklik Tilsimi! Ceviklik +5!";
                            }
                            break;
                        case ConsoleKey.D7:
                            if (diamond >= 10) 
                            { 
                                diamond -= 10; 
                                maxHealth += 50;
                                health = maxHealth;
                                status = "Can Iksiri! Max can +50!";
                            }
                            break;
                    }
                }
            }
        }

        // ==================== BÜYÜCÜ MENÜSÜ ====================
        static void Wizard()
        {
            OpenPage("wizard");
            
            while (inMenu && pageStack.Peek() == "wizard")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("              B U Y U C U                  ");
                Console.WriteLine("═══════════════════════════════════════════");
                Console.ResetColor();
                
                Console.WriteLine("\n [KAYNAKLARIN]");
                Console.WriteLine($" Elmas: {diamond} | Komur: {coal}");
                Console.WriteLine($"\n [BUYULER]");
                Console.WriteLine(" ─────────────────────────────────────────");
                Console.WriteLine($" 1. Ates Topu       (10 Elmas)    {(unlockedSkills.Contains("ates_topu") ? "VAR" : "YOK")}");
                Console.WriteLine($" 2. Buz Kilici      (15 Elmas)    Guc +15");
                Console.WriteLine($" 3. Ruzgar Oku      (10 Elmas)    Ceviklik +15");
                Console.WriteLine($" 4. Dirilis         (20 Elmas)    Max Can +100");
                Console.WriteLine($" 5. Isik Zirhi      (15 Elmas)    Zirh +3");
                Console.WriteLine($"\n [ESC] Geri Don");
                
                var k = Console.ReadKey(true).Key;
                
                if (k == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else
                {
                    switch (k)
                    {
                        case ConsoleKey.D1:
                            if (diamond >= 10 && !unlockedSkills.Contains("ates_topu")) 
                            { 
                                diamond -= 10;
                                unlockedSkills.Add("ates_topu");
                                status = "Ates Topu ogrendin!";
                            }
                            break;
                        case ConsoleKey.D2:
                            if (diamond >= 15) 
                            { 
                                diamond -= 15;
                                strength += 15;
                                status = "Buz Kilici! Guc +15!";
                            }
                            break;
                        case ConsoleKey.D3:
                            if (diamond >= 10) 
                            { 
                                diamond -= 10;
                                dexterity += 15;
                                status = "Ruzgar Oku! Ceviklik +15!";
                            }
                            break;
                        case ConsoleKey.D4:
                            if (diamond >= 20) 
                            { 
                                diamond -= 20;
                                maxHealth += 100;
                                health = maxHealth;
                                status = "Dirilis! Max can +100!";
                            }
                            break;
                        case ConsoleKey.D5:
                            if (diamond >= 15) 
                            { 
                                diamond -= 15;
                                armorLevel += 3;
                                status = "Isik Zirhi! Zirh +3!";
                            }
                            break;
                    }
                }
            }
        }

        // ==================== SAVE/LOAD SİSTEMİ ====================
        static void SaveGame()
        {
            try
            {
                string saveFolder = "Saves";
                if (!Directory.Exists(saveFolder))
                    Directory.CreateDirectory(saveFolder);
                
                string saveData = $"{px},{py},{health},{maxHealth},{hunger},{level},{xp}," +
                                 $"{strength},{dexterity},{stamina},{maxStamina}," +
                                 $"{stone},{wood},{coal},{iron},{diamond},{arrows}," +
                                 $"{hasBow},{hasShield},{armorLevel},{pickaxeLevel}," +
                                 $"{worldTime},{combatStats["canavar_oldurme"]},{combatStats["toplam_hasar"]}," +
                                 $"{combatStats["kritik_vurus"]},{combatStats["kombolar"]}," +
                                 $"{string.Join("|", unlockedSkills)},{DateTime.Now.Ticks}";
                
                File.WriteAllText(Path.Combine(saveFolder, "quicksave.txt"), saveData);
                
                string backupFile = Path.Combine(saveFolder, $"save_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(backupFile, saveData);
                
                status = "Oyun kaydedildi!";
            }
            catch (Exception ex)
            {
                status = $"Kayit hatasi: {ex.Message}";
            }
        }

        static void LoadGame()
        {
            string saveFolder = "Saves";
            string savePath = Path.Combine(saveFolder, "quicksave.txt");
            
            if (File.Exists(savePath))
            {
                try
                {
                    string[] s = File.ReadAllText(savePath).Split(',');
                    
                    px = int.Parse(s[0]); py = int.Parse(s[1]);
                    health = double.Parse(s[2]); maxHealth = double.Parse(s[3]);
                    hunger = double.Parse(s[4]); level = int.Parse(s[5]); xp = int.Parse(s[6]);
                    strength = int.Parse(s[7]); dexterity = int.Parse(s[8]);
                    stamina = int.Parse(s[9]); maxStamina = int.Parse(s[10]);
                    stone = int.Parse(s[11]); wood = int.Parse(s[12]);
                    coal = int.Parse(s[13]); iron = int.Parse(s[14]);
                    diamond = int.Parse(s[15]); arrows = int.Parse(s[16]);
                    hasBow = bool.Parse(s[17]); hasShield = bool.Parse(s[18]);
                    armorLevel = int.Parse(s[19]); pickaxeLevel = int.Parse(s[20]);
                    worldTime = int.Parse(s[21]);
                    
                    combatStats["canavar_oldurme"] = int.Parse(s[22]);
                    combatStats["toplam_hasar"] = int.Parse(s[23]);
                    combatStats["kritik_vurus"] = int.Parse(s[24]);
                    combatStats["kombolar"] = int.Parse(s[25]);
                    
                    if (s.Length > 26)
                    {
                        string[] skills = s[26].Split('|', StringSplitOptions.RemoveEmptyEntries);
                        unlockedSkills = new List<string>(skills);
                        if (!unlockedSkills.Contains("vurus"))
                            unlockedSkills.Add("vurus");
                    }
                    
                    GenerateWorld();
                    status = "Kayit yuklendi!";
                }
                catch
                {
                    status = "Yukleme hatasi! Yeni oyun baslatiliyor...";
                    ResetGame();
                    GenerateWorld();
                }
            }
            else
            {
                ResetGame();
                GenerateWorld();
            }
        }

        static void SaveMenu()
        {
            OpenPage("save");
            
            string saveFolder = "Saves";
            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);
            
            while (inMenu && pageStack.Peek() == "save")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("          KAYIT YONETICISI                 ");
                Console.WriteLine("═══════════════════════════════════════════");
                Console.ResetColor();
                
                var saveFiles = Directory.GetFiles(saveFolder, "*.txt")
                                         .OrderByDescending(f => File.GetLastWriteTime(f))
                                         .ToList();
                
                if (saveFiles.Count == 0)
                {
                    Console.WriteLine("\nHenuz kayit bulunmuyor.");
                }
                else
                {
                    Console.WriteLine($"\n{saveFiles.Count} kayit dosyasi bulundu:\n");
                    
                    for (int i = 0; i < Math.Min(saveFiles.Count, 10); i++)
                    {
                        string file = saveFiles[i];
                        FileInfo info = new FileInfo(file);
                        
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"[{i + 1}] ");
                        Console.ResetColor();
                        Console.Write($"{Path.GetFileName(file),-30} ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"{info.LastWriteTime:dd/MM/yy HH:mm}");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }
                
                Console.WriteLine("\n───────────────────────────────────────────");
                Console.WriteLine("[1-9] Kayit Yukle  |  [S] Hizli Kaydet");
                Console.WriteLine("[D] Kayit Sil      |  [Y] Yeni Kayit");
                Console.WriteLine("[ESC] Geri Don     |  [M] Ana Menu");
                Console.WriteLine("───────────────────────────────────────────");
                
                var key = Console.ReadKey(true).Key;
                
                if (key == ConsoleKey.Escape)
                {
                    ClosePage();
                }
                else if (key == ConsoleKey.M)
                {
                    ReturnToGame();
                    OpenPage("mainmenu");
                    ShowMainMenu();
                }
                else if (key == ConsoleKey.S)
                {
                    SaveGame();
                    Console.SetCursorPosition(0, 20);
                    Console.WriteLine("Hizli kayit alindi!");
                    System.Threading.Thread.Sleep(500);
                }
                else if (key == ConsoleKey.Y)
                {
                    SaveGame();
                    Console.SetCursorPosition(0, 20);
                    Console.WriteLine("Yeni kayit olusturuldu!");
                    System.Threading.Thread.Sleep(500);
                }
                else if (key == ConsoleKey.D)
                {
                    if (saveFiles.Count > 0)
                    {
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("KAYIT SILME");
                        Console.ResetColor();
                        Console.WriteLine("Hangi kaydi silmek istiyorsun?\n");
                        
                        for (int i = 0; i < Math.Min(saveFiles.Count, 10); i++)
                        {
                            Console.WriteLine($"[{i + 1}] {Path.GetFileName(saveFiles[i])}");
                        }
                        Console.WriteLine("\n[0] Iptal");
                        
                        var delKey = Console.ReadKey(true).Key;
                        if (delKey >= ConsoleKey.D1 && delKey <= ConsoleKey.D9)
                        {
                            int index = (int)delKey - (int)ConsoleKey.D1;
                            if (index < saveFiles.Count)
                            {
                                File.Delete(saveFiles[index]);
                                Console.WriteLine($"\nKayit silindi!");
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                    }
                }
                else if (key >= ConsoleKey.D1 && key <= ConsoleKey.D9)
                {
                    int index = (int)key - (int)ConsoleKey.D1;
                    if (index < saveFiles.Count)
                    {
                        string selectedSave = saveFiles[index];
                        File.Copy(selectedSave, Path.Combine(saveFolder, "quicksave.txt"), true);
                        ClosePage();
                        LoadGame();
                    }
                }
            }
        }

        static void ResetGame()
        {
            health = 100; maxHealth = 100; hunger = 100;
            stone = 50; wood = 50; coal = 0; iron = 0; diamond = 0; arrows = 5;
            level = 1; xp = 0; strength = 10; dexterity = 10;
            stamina = 100; maxStamina = 100;
            hasBow = false; hasShield = false; armorLevel = 0; pickaxeLevel = 1;
            unlockedSkills = new List<string> { "vurus" };
            worldTime = 800;
            px = 250; py = 250;
            
            InitializeCombatStats();
        }

        static void GameOver()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
    ╔══════════════════════════════════════════════════════════╗
    ║                                                          ║
    ║                    OYUN BITTI                           ║
    ║                                                          ║
    ╠══════════════════════════════════════════════════════════╣
    ║                                                          ║");
            Console.WriteLine($"    ║              Seviye: {level,-32}║");
            Console.WriteLine($"    ║              Canavar: {combatStats["canavar_oldurme"],-30}║");
            Console.WriteLine($"    ║              Hasar: {combatStats["toplam_hasar"],-32}║");
            Console.WriteLine($"    ║              Sure: {(DateTime.Now - gameStartTime).TotalMinutes:F0} dakika    ║");
            Console.WriteLine(@"
    ║                                                          ║
    ║              [1] YENIDEN BASLA                          ║
    ║              [2] SON KAYITTI YUKLE                      ║
    ║              [3] CIKIS                                  ║
    ║                                                          ║
    ╚══════════════════════════════════════════════════════════╝
            ");
            Console.ResetColor();
            
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.D1)
            {
                ResetGame();
                GenerateWorld();
                Main(new string[0]);
            }
            else if (key == ConsoleKey.D2)
            {
                LoadGame();
            }
            else if (key == ConsoleKey.D3)
            {
                Environment.Exit(0);
            }
        }

        static void AutoSave()
        {
            if (!inCombat && !inMenu)
            {
                autoSaveTimer++;
                if (autoSaveTimer > 3000)
                {
                    SaveGame();
                    autoSaveTimer = 0;
                    status = "Otomatik kayit alindi!";
                }
            }
        }
    }
}