def example(n,a):
    if n > 0:
        for i in range(a):
            print("d")
            example(n - 1, a)

example(8, 6)