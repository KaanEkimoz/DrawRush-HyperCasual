using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private int damage = -1;
    public Animator enemyAnim;
    private PlayerCombat _playerCombat;

    private void Awake()
    {
        if (enemyAnim == null)
        {
            enemyAnim = GetComponentInChildren<Animator>();
        }

        if (_playerCombat == null)
        {
            _playerCombat = FindObjectOfType<PlayerCombat>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !GameManager.isGameWon)
        {
            _playerCombat.TakeDamage(damage);
        }
    }
}
