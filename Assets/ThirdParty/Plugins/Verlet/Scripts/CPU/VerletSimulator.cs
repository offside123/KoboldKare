﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class VerletSimulator {

        List<Node> particles;

        public VerletSimulator(List<Node> particles)
        {
            this.particles = particles;
        }
       
        public void Simulate (int iterations, float dt) {
            Step();
            Solve(iterations, dt);
        }

        void Step() {
            foreach (Node p in particles) {
                p.Step();
            }
        }

        void Solve(int iterations, float dt) {
            for(int iter = 0; iter < iterations; iter++) {
                foreach(Node p in particles) {
                    Solve(p);
                }
            }
        }

        void Solve(Node particle) {
            foreach (Edge e in particle.Connection) {
                var other = e.Other(particle);
                Solve(particle, other, e.Length);
            }
        }

        void Solve(Node a, Node b, float rest)
        {
            var delta = a.position - b.position;
            var current = delta.magnitude;
            var f = (current - rest) / Mathf.Max(current,0.01f);
            a.position -= f * 0.5f * delta;
            b.position += f * 0.5f * delta;
        }

        public void DrawGizmos() {
            for(int i = 0, n = particles.Count; i < n; i++)
            {
                var p = particles[i];
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(p.position, 0.2f);

                Gizmos.color = Color.white;
                p.Connection.ForEach(e => {
                    var other = e.Other(p);
                    Gizmos.DrawLine(p.position, other.position);
                });
            }
        }

    }

}


