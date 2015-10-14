using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace Atlas
{
    /// <summary>
    /// The player class, which the user controls
    /// </summary>
    class Player : Character
    {
        /// <summary>
        /// Lists of sprites used by the player object in various situations
        /// </summary>
        private List<Image> linkUpIdle;
        private List<Image> linkDownIdle;
        private List<Image> linkLeftIdle;
        private List<Image> linkRightIdle;

        private List<Image> linkUpRun;
        private List<Image> linkDownRun;
        private List<Image> linkLeftRun;
        private List<Image> linkRightRun;

        private List<Image> linkUpBlock;
        private List<Image> linkDownBlock;
        private List<Image> linkLeftBlock;
        private List<Image> linkRightBlock;

        private List<Image> linkUpAttack;
        private List<Image> linkDownAttack;
        private List<Image> linkLeftAttack;
        private List<Image> linkRightAttack;

        /// <summary>
        /// Bool telling the player if he has a sword, which allows him to attack
        /// </summary>
        public bool hasItemSword = true;
        
        /// <summary>
        /// Bool telling the player if he has a sheidl, which allows him to block
        /// </summary>
        public bool hasItemShield = true;

        /// <summary>
        /// The speed at which the player moves
        /// </summary>
        private float movementSpeedRun = 96;

        /// <summary>
        /// Player instance field variable
        /// </summary>
        private static Player instance;

        /// <summary>
        /// Returns or instantiates Player instance singleton
        /// </summary>
        public static Player Instance
        {
            get 
            { 
                if (instance == null)
                    instance = new Player();
				
                return instance;
            }
        }
        
        /// <summary>
        /// The constructer defining the values of certain variables
        /// </summary>
        private Player()
        {
            this.updateOutsideCam = true;

            this.position = position;
            this.positionPrev = position;
            this.attackRange = 30;
            this.health = maxHealth;
            this.solid = true;
            this.spriteOrigin = new Vector2(23, 30);
            this.spriteAnimated = true;
            this.spriteSpeed = 8;

            // Fill sprite lists
            linkUpIdle = FillSpriteList("link", "up", "idle");
            linkDownIdle = FillSpriteList("link", "down", "idle");
            linkLeftIdle = FillSpriteList("link", "left", "idle");
            linkRightIdle = FillSpriteList("link", "right", "idle");

            linkUpRun = FillSpriteList("link", "up", "run");
            linkDownRun = FillSpriteList("link", "down", "run");
            linkLeftRun = FillSpriteList("link", "left", "run");
            linkRightRun = FillSpriteList("link", "right", "run");

            linkUpBlock = FillSpriteList("link", "up", "block");
            linkDownBlock = FillSpriteList("link", "down", "block");
            linkLeftBlock = FillSpriteList("link", "left", "block");
            linkRightBlock = FillSpriteList("link", "right", "block");

            linkUpAttack = FillSpriteList("link", "up", "attack");
            linkDownAttack = FillSpriteList("link", "down", "attack");
            linkLeftAttack = FillSpriteList("link", "left", "attack");
            linkRightAttack = FillSpriteList("link", "right", "attack");
        }

        /// <summary>
        /// Update function handling movement, combat commands and interaction command
        /// </summary>
        public override void Update()
        {
            if (Game.gamePaused)
                return;

            base.Update();

            if (attackCooldown > 0)
            {
                attackCooldown -= Time.DeltaTime;
                isAttacking = true;
            }
            else
                isAttacking = false;

            if (!knockback)
            {
                Vector2 inputVector = Input.GetAxis();
                movementTarget = position + inputVector;
            
                if (Input.GetKeyDown(Keys.Q) && attackCooldown <= 0 && !isBlocking && hasItemSword)
                {
                    Attack();
                    spriteIndex = 0;
                    attackCooldown = 0.17f;
                }

                if (Input.GetKey(Keys.W) && hasItemShield)
                {
                    if (!isBlocking)
                        Sound.Play("plrShield");

                    isBlocking = true;
                }
                else
                    isBlocking = false;

                if (Input.GetKeyDown(Keys.E) && !isAttacking)
                    CallInteract();
            }

            // Animates Player
            spriteFrames = Animate().ToList();

            if (isBlocking)
                movementSpeed = movementSpeedRun / 2;
            else
                movementSpeed = movementSpeedRun;

            //updates position(movement) and direction. 
            if (isAttacking)
                movementTarget = position;
        }

        /// <summary>
        /// Animates the player, giving him the relevant sprite for the given situation
        /// </summary>
        private List<Image> Animate()
        {
            bool isMoving = !position.Equals(positionPrev);
            spriteSpeed = 8;

            if (isBlocking && !isMoving)
                spriteSpeed = 0;
            else if (isAttacking)
                spriteSpeed = 30;

            if (Math.Abs(direction) > 140)
            {
                if (isBlocking)
                    return linkLeftBlock;
                else if (isAttacking)
                    return linkLeftAttack;
                else if (isMoving)
                    return linkLeftRun;
                else
                    return linkLeftIdle;
            }
            else if (Math.Abs(direction) > 40)
            {
                if (direction > 0)
                {
                    if (isBlocking)
                        return linkDownBlock;
                    else if (isAttacking)
                        return linkDownAttack;
                    else if (isMoving)
                        return linkDownRun;
                    else
                        return linkDownIdle;
                }
                else
                {
                    if (isBlocking)
                        return linkUpBlock;
                    else if (isAttacking)
                        return linkUpAttack;
                    else if (isMoving)
                        return linkUpRun;
                    else
                        return linkUpIdle;
                }
            }
            else
            {
                if (isBlocking)
                    return linkRightBlock;
                else if (isAttacking)
                    return linkRightAttack;
                else if (isMoving)
                    return linkRightRun;
                else
                    return linkRightIdle;
            }
        }

        /// <summary>
        /// the players attack funtion dealing damage to enemies in a radius
        /// </summary>
        public override void Attack()
        {
            isAttacking = true;
            List<Enemy> attackTargets = new List<Enemy>();
            attackTargets = Game.gameObjectsChanged.OfType<Enemy>().ToList();

            Vector2 plrForward = Vector2.AngleToVec(this.direction).normalized;

            foreach (Enemy enemy in attackTargets)
            {
                if (Vector2.Distance(this.position, enemy.position) > this.attackRange)
                    continue;

                Vector2 vecToEnemy = (enemy.position - position).normalized;

                if (Vector2.Angle(plrForward, vecToEnemy) < 60)
                    enemy.TakeDamage(this.attackDamage, this.position, this.attackPower);
            }

            // Sound
            Sound.Play("plrSword");
        }

        /// <summary>
        /// The players interaction command, calling the interact function for nearby objects
        /// </summary>
        public void CallInteract()
        {
            List<GameObject> interactTargets = new List<GameObject>();
            interactTargets = Game.gameObjectsChanged.OfType<GameObject>().ToList();

            charForward = Vector2.AngleToVec(this.direction).normalized;

            foreach (GameObject interactable in interactTargets)
            {
                if (Vector2.Distance(this.position, interactable.position) > this.interactionRange)
                    continue;

                vecToOther = (interactable.position - position).normalized;

                if (Vector2.Angle(charForward, vecToOther) < 30)
                    interactable.Interact();
            }
        }

        /// <summary>
        /// The plays die function
        /// </summary>
        public override void Die()
        {
            base.Die();

            Game.gameRestart = true;
        }

    }
}
