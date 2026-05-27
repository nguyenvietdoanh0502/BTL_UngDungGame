using UnityEngine;

public class EnemyController : MonoBehaviour
{
    Rigidbody2D rigidbody2D;
    public float speed = 3.0f;
    public float changeTime = 3.0f;
    float timer;
    Animator animator;
   	int direction = 1;
    bool broken = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        timer = changeTime;
        
       	animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        timer-= Time.deltaTime;

      		if (timer < 0)
      		{
        		direction = -direction;
        		timer = changeTime;
      		}
        if (direction > 0)
        {
            transform.localScale = new Vector3(1,1,1);
        }
        else
        {
            transform.localScale = new Vector3(-1,1,1);
        }
        
    }
    void FixedUpdate()
    {
        Vector2 position = rigidbody2D.position;
        if (!broken)
        {
            animator.SetBool("isRunning",false);
            animator.SetBool("Farm",true);
            return;
        }
        animator.SetBool("isRunning",true);
        position.x = position.x + speed * direction * Time.deltaTime;
        rigidbody2D.MovePosition(position);
    }
    public void Fix()
    {
        broken = false;
        rigidbody2D.simulated = false;
    }
}
