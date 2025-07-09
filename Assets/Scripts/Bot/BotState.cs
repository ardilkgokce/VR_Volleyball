using UnityEngine;

// Bot State Base Class
public abstract class BotState
{
    protected BotController bot;
    protected Transform transform;
    
    public BotState(BotController botController)
    {
        bot = botController;
        transform = bot.transform;
    }
    
    // Her state'in implement etmesi gereken metodlar
    public abstract void Enter();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void Exit();
    
    // Ortak yardımcı metodlar
    protected void LookAtTarget(Vector3 targetPosition)
    {
        if (bot.animationController != null)
        {
            bot.animationController.LookAt(targetPosition, bot.rotationSpeed);
        }
        else
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bot.rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    protected void UpdateAnimationSpeed(float speed, bool isMoving)
    {
        if (bot.animationController != null)
        {
            bot.animationController.UpdateMovementAnimation(speed, isMoving);
        }
    }
}