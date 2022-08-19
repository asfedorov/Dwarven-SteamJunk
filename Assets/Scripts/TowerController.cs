using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerController : DieableController
{
    public float timeout = 2f;
    float _timeout = 0f;

    public GameObject player;

    public GameObject projectilePrefab;

    public float radius = 12f;

    GameObject projectileReady;

    GameObject world;

    bool powered = false;

    Collider2D collider;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        world = GameObject.FindGameObjectWithTag("World");
    }

    public void PowerOn()
    {
        powered = true;
        player = GameObject.FindGameObjectWithTag("Player");
        world = GameObject.FindGameObjectWithTag("World");
        collider = GetComponent<Collider2D>();
        CreateNewProjectile();
    }

    public void PowerOff()
    {
        powered = false;
        Destroy(projectileReady);
    }

    // Update is called once per frame
    void Update()
    {
        if (powered)
        {
            if (_timeout <= 0f)
            {
                if (Vector3.Distance(transform.position, player.transform.position) < radius)
                {
                    // Debug.Log("Player in radius");
                    if (TowerCanSeePlayer())
                    {
                        // Debug.Log("Player spotted");

                        Shoot(player.transform.position);
                        CreateNewProjectile();
                    }
                }
            }
            else
            {
                _timeout -= Time.deltaTime;
            }
        }
    }

    bool TowerCanSeePlayer()
    {
        // Ray ray = new Ray(transform.position, player.transform.position - transform.position);
        RaycastHit2D hit;
        RaycastHit2D[] hits = new RaycastHit2D[1];

        Vector3 weaponPos = transform.position + new Vector3(2f, 6f, 0f);
        Vector3 direction = player.transform.position - weaponPos;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(~LayerMask.GetMask("Buildings"));

        if (
            Physics2D.Raycast(
                new Vector2(weaponPos.x, weaponPos.y),
                new Vector2(direction.x, direction.y),
                filter,
                hits,
                radius
            ) > 0
        )
        {
            if (hits[0].collider == null)
            {
                return false;
            }

            // Debug.Log("Something spotted");

            if (hits[0].transform == player.transform)
            {
                return true;
            }

        }

        return false;
    }

    void Shoot(Vector3 target)
    {
        ProjectileController pc = projectileReady.GetComponent<ProjectileController>();
        pc.SetDirection(target - (transform.position + new Vector3(2f, 6f, 0f)));
        projectileReady = null;

    }

    void CreateNewProjectile()
    {
        projectileReady = Instantiate(projectilePrefab, transform.position + new Vector3(2f, 6f, 0f), Quaternion.identity, world.transform);
        Collider2D prc = projectileReady.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(collider, prc, true);
        _timeout = timeout;

    }

}
